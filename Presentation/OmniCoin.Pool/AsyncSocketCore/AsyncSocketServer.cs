using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using FiiiChain.Framework;
using FiiiChain.PoolMessages;
using System.Collections.Generic;
using FiiiChain.Pool.Commands;

namespace FiiiChain.Pool
{
    /// <summary>
    /// 服务入口，建立Socket监听，负责接收连接，
    /// 绑定连接对象，处理异步事件返回的接收和发送事件
    /// </summary>
    public class AsyncSocketServer
    {
        private Socket listenSocket;
        /// <summary>
        /// 最大支持连接个数
        /// </summary>
        private int m_numConnections;
        /// <summary>
        /// 每个连接接收缓存大小
        /// </summary>
        private int m_receiveBufferSize;
        /// <summary>
        /// 限制访问接收连接的线程数，用来控制最大并发数
        /// </summary>
        private Semaphore m_maxNumberAcceptedClients;
        /// <summary>
        /// Socket最大超时时间，单位为MS
        /// </summary>
        private int m_socketTimeOutMS;
        public int SocketTimeOutMS { get { return m_socketTimeOutMS; } set { m_socketTimeOutMS = value; } }
        /// <summary>
        /// 管理所有空闲的AsyncSocketUserToken，采用栈的管理方式，后进先出
        /// </summary>
        private AsyncSocketUserTokenPool m_asyncSocketUserTokenPool;
        /// <summary>
        /// 管理所有正在执行的AsyncSocketUserToken，是一个列表
        /// </summary>
        private AsyncSocketUserTokenList m_asyncSocketUserTokenList;
        /// <summary>
        /// 管理所有正在执行的AsyncSocketUserToken，是一个列表
        /// </summary>
        public AsyncSocketUserTokenList AsyncSocketUserTokenList { get { return m_asyncSocketUserTokenList; } }

        public int ConnectionCount;

        /// <summary>
        /// 守护进程，用于关闭超时连接
        /// </summary>
        private DaemonThread m_daemonThread;

        public Action<AsyncSocketUserToken, PoolCommand> ReceivedCommandAction { get; set; }
        public Func<AsyncSocketUserToken, bool, bool> ReceivedMinerConnectionAction { get; set; }

        /// <summary>
        /// SocketAsyncEventArgs封装和MSDN的不同点
        /// MSDN在http://msdn.microsoft.com/zh-cn/library/system.net.sockets.socketasynceventargs(v=vs.110).aspx
        /// 实现了示例代码，并实现了初步的池化处理，我们是在它的基础上扩展实现了接收数据缓冲，发送数据队列
        /// ，并把发送SocketAsyncEventArgs和接收SocketAsyncEventArgs分开，
        /// 并实现了协议解析单元，这样做的好处是方便后续逻辑实现文件的上传，下载和日志输出。
        /// </summary>
        /// <param name="numConnections"></param>
        public AsyncSocketServer(int numConnections, int receiveBufferSize)
        {
            m_numConnections = numConnections;
            m_receiveBufferSize = receiveBufferSize;

            m_asyncSocketUserTokenPool = new AsyncSocketUserTokenPool(numConnections);
            m_asyncSocketUserTokenList = new AsyncSocketUserTokenList();
            m_maxNumberAcceptedClients = new Semaphore(numConnections, numConnections);
        }
        /// <summary>
        /// 按照连接数建立读写对象
        /// </summary>
        public void Init()
        {
            AsyncSocketUserToken userToken;
            for (int i = 0; i < m_numConnections; i++)
            {
                userToken = new AsyncSocketUserToken(m_receiveBufferSize);
                userToken.ReceiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                userToken.SendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                m_asyncSocketUserTokenPool.Push(userToken);
            }
        }
        /// <summary>
        /// 建立一个Socket监听对象
        /// </summary>
        /// <param name="localEndPoint"></param>
        public void Start(IPEndPoint localEndPoint)
        {
            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            listenSocket.Listen(m_numConnections);
            LogHelper.Info(string.Format("Start listen socket {0} success", localEndPoint.ToString()));
            //for (int i = 0; i < 64; i++) //不能循环投递多次AcceptAsync，会造成只接收8000连接后不接收连接了
            StartAccept(null);
            m_daemonThread = new DaemonThread(this);
        }
        /// <summary>
        /// 开始接受连接，SocketAsyncEventArgs有连接时会通过Completed事件通知外面
        /// </summary>
        /// <param name="acceptEventArgs"></param>
        public void StartAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            if (acceptEventArgs == null)
            {
                acceptEventArgs = new SocketAsyncEventArgs();
                acceptEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                acceptEventArgs.AcceptSocket = null; //释放上次绑定的Socket，等待下一个Socket连接
            }

            m_maxNumberAcceptedClients.WaitOne(); //获取信号量
            bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArgs);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArgs);
            }
        }
        /// <summary>
        /// 接受连接响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="acceptEventArgs"></param>
        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs acceptEventArgs)
        {
            try
            {
                ProcessAccept(acceptEventArgs);
            }
            catch (Exception E)
            {
                LogHelper.Error(string.Format("Accept client {0} error, message: {1}", acceptEventArgs.AcceptSocket, E.Message));
                LogHelper.Error(E.StackTrace);
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            LogHelper.Info(string.Format("Client connection accepted. Local Address: {0}, Remote Address: {1}",
                acceptEventArgs.AcceptSocket.LocalEndPoint, acceptEventArgs.AcceptSocket.RemoteEndPoint));
            Interlocked.Increment(ref ConnectionCount);
            AsyncSocketUserToken userToken = m_asyncSocketUserTokenPool.Pop();
            m_asyncSocketUserTokenList.Add(userToken); //添加到正在连接列表
            userToken.ConnectSocket = acceptEventArgs.AcceptSocket;
            userToken.ConnectDateTime = DateTime.Now;
            userToken.Address = acceptEventArgs.AcceptSocket.RemoteEndPoint.ToString();

            if (this.ReceivedMinerConnectionAction != null)
            {
                this.ReceivedMinerConnectionAction(userToken, true);
            }

            try
            {
                bool willRaiseEvent = userToken.ConnectSocket.ReceiveAsync(userToken.ReceiveEventArgs); //投递接收请求
                if (!willRaiseEvent)
                {
                    lock (userToken)
                    {
                        ProcessReceive(userToken.ReceiveEventArgs);
                    }
                }
            }
            catch (Exception E)
            {
                LogHelper.Error(string.Format("Accept client {0} error, message: {1}", userToken.ConnectSocket, E.Message));
                LogHelper.Error(E.StackTrace);
            }

            StartAccept(acceptEventArgs); //把当前异步事件释放，等待下次连接
        }
        /// <summary>
        /// NET底层IO线程也是每个异步事件都是由不同的线程返回到Completed事件，
        /// 因此在Completed事件需要对用户对象进行加锁，
        /// 避免同一个用户对象同时触发两个Completed事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="asyncEventArgs"></param>
        void IO_Completed(object sender, SocketAsyncEventArgs asyncEventArgs)
        {
            AsyncSocketUserToken userToken = asyncEventArgs.UserToken as AsyncSocketUserToken;
            userToken.ActiveDateTime = DateTime.Now;
            try
            {
                lock (userToken)
                {//避免同一个userToken同时有多个线程操作
                    if (asyncEventArgs.LastOperation == SocketAsyncOperation.Receive)
                        ProcessReceive(asyncEventArgs);
                    else if (asyncEventArgs.LastOperation == SocketAsyncOperation.Send)
                        ProcessSend(asyncEventArgs);
                    else
                        throw new ArgumentException("The last operation completed on the socket was not a receive or send");
                }
            }
            catch (Exception E)
            {
                LogHelper.Error(string.Format("IO_Completed {0} error, message: {1}", userToken.ConnectSocket, E.Message));
                LogHelper.Error(E.StackTrace);
            }
        }
        /// <summary>
        /// 接收事件响应函数,接收的逻辑
        /// </summary>
        /// <param name="receiveEventArgs"></param>
        private void ProcessReceive(SocketAsyncEventArgs receiveEventArgs)
        {
            AsyncSocketUserToken userToken = receiveEventArgs.UserToken as AsyncSocketUserToken;
            if (userToken.ConnectSocket == null)
                return;
            userToken.ActiveDateTime = DateTime.Now;
            if (userToken.ReceiveEventArgs.BytesTransferred > 0 && userToken.ReceiveEventArgs.SocketError == SocketError.Success)
            {
                HeartbeatCommand.UpdateHeartTime(userToken);

                int offset = userToken.ReceiveEventArgs.Offset;
                int count = userToken.ReceiveEventArgs.BytesTransferred;

                if (count > 0) //处理接收数据
                {
                    var buffer = userToken.ReceiveEventArgs.Buffer;
                    var commandDataList = new List<byte[]>();
                    var index = 0;
                    List<byte> bytes = null;

                    while (index < buffer.Length)
                    {
                        if (bytes == null)
                        {
                            if ((index + 3) < buffer.Length &&
                            buffer[index] == PoolCommand.DefaultPrefixBytes[0] &&
                            buffer[index + 1] == PoolCommand.DefaultPrefixBytes[1] &&
                            buffer[index + 2] == PoolCommand.DefaultPrefixBytes[2] &&
                            buffer[index + 3] == PoolCommand.DefaultPrefixBytes[3])
                            {
                                bytes = new List<byte>();
                                bytes.AddRange(PoolCommand.DefaultPrefixBytes);
                                index += 4;
                            }
                            else
                            {
                                index++;
                            }
                        }
                        else
                        {
                            if ((index + 3) < buffer.Length &&
                            buffer[index] == PoolCommand.DefaultSuffixBytes[0] &&
                            buffer[index + 1] == PoolCommand.DefaultSuffixBytes[1] &&
                            buffer[index + 2] == PoolCommand.DefaultSuffixBytes[2] &&
                            buffer[index + 3] == PoolCommand.DefaultSuffixBytes[3])
                            {
                                bytes.AddRange(PoolCommand.DefaultSuffixBytes);
                                commandDataList.Add(bytes.ToArray());
                                bytes = null;

                                index += 4;
                            }
                            else
                            {
                                bytes.Add(buffer[index]);
                                index++;
                            }
                        }
                    }

                    if (this.ReceivedCommandAction != null)
                    {
                        foreach (var data in commandDataList)
                        {
                            try
                            {
                                var cmd = PoolCommand.ConvertBytesToMessage(data);
                                if (cmd != null)
                                {
                                    this.ReceivedCommandAction(userToken, cmd);
                                }
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Warn($"Error Data from {userToken.Address}：{Base16.Encode(data)}");
                                LogHelper.Error("Error occured on deserialize messgae: " + ex.Message, ex);
                            }
                        }
                    }

                    if (userToken.ConnectSocket == null || userToken.ReceiveEventArgs == null)
                        return;

                    bool willRaiseEvent = userToken.ConnectSocket.ReceiveAsync(userToken.ReceiveEventArgs); //投递接收请求
                    if (!willRaiseEvent)
                        ProcessReceive(userToken.ReceiveEventArgs);
                }
                else
                {
                    CloseSocket(userToken);
                }
            }
        }
        /// <summary>
        /// 发送事件响应函数,发送的逻辑,把发送数据放到一个列表中，当上一个发送事件完成响应Completed事件，
        /// 这时我们需要检测发送队列中是否存在未发送的数据，如果存在则继续发送
        /// </summary>
        /// <param name="sendEventArgs"></param>
        /// <returns></returns>
        private bool ProcessSend(SocketAsyncEventArgs sendEventArgs)
        {
            AsyncSocketUserToken userToken = sendEventArgs.UserToken as AsyncSocketUserToken;
            userToken.ActiveDateTime = DateTime.Now;
            if (sendEventArgs.SocketError == SocketError.Success)
                return true;
            else
            {
                CloseSocket(userToken);
                return false;
            }
        }

        public void SendCommand(AsyncSocketUserToken token, PoolCommand command)
        {
            try
            {
                var buffer = command.GetBytes();
                this.SendAsyncEvent(token.ConnectSocket, token.SendEventArgs, buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
                this.CloseSocket(token);
            }
        }

        public bool SendAsyncEvent(Socket connectSocket, SocketAsyncEventArgs sendEventArgs, byte[] buffer, int offset, int count)
        {
            if (connectSocket == null)
                return false;
            sendEventArgs.SetBuffer(buffer, offset, count);
            bool willRaiseEvent = connectSocket.SendAsync(sendEventArgs);
            if (!willRaiseEvent)
            {
                return ProcessSend(sendEventArgs);
            }
            else
                return true;
        }
        /// <summary>
        /// 当一个SocketAsyncEventArgs断开后，我们需要断开对应的Socket连接，并释放对应资源
        /// </summary>
        /// <param name="userToken"></param>
        public void CloseSocket(AsyncSocketUserToken userToken)
        {
            if (userToken.ConnectSocket == null)
                return;

            Interlocked.Decrement(ref ConnectionCount);
            string socketInfo = string.Format("Local Address: {0} Remote Address: {1}", userToken.ConnectSocket.LocalEndPoint,
                userToken.ConnectSocket.RemoteEndPoint);
            LogHelper.Info(string.Format("Client connection disconnected. {0}", socketInfo));


            if (this.ReceivedMinerConnectionAction != null)
            {
                this.ReceivedMinerConnectionAction(userToken, true);
            }

            try
            {
                userToken.ConnectSocket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception E)
            {
                LogHelper.Error(string.Format("CloseClientSocket Disconnect client {0} error, message: {1}", socketInfo, E.Message));
            }
            userToken.ConnectSocket.Close();
            userToken.ConnectSocket = null; //释放引用，并清理缓存，包括释放协议对象等资源

            m_maxNumberAcceptedClients.Release();
            m_asyncSocketUserTokenPool.Push(userToken);
            m_asyncSocketUserTokenList.Remove(userToken);
        }
    }
}
