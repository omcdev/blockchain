using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using FiiiChain.PoolMessages;
using FiiiChain.Framework;

namespace FiiiChain.MinerTest
{
    class SocketClient : IDisposable
    {
        // Constants for socket operations.
        private const Int32 ReceiveOperation = 1, SendOperation = 0;

        // The socket used to send/receive messages.
        public Socket ClientSocket;

        // Flag for connected socket.
        private Boolean connected = false;

        // Listener endpoint.
        public IPEndPoint HostEndPoint;

        SocketAsyncEventArgs sendArgs = null;
        SocketAsyncEventArgs receiveArgs = null;

        // Signals a connection.
        private static AutoResetEvent autoConnectEvent =
                              new AutoResetEvent(false);

        // Signals the send/receive operation.
        private static AutoResetEvent[]
                autoSendReceiveEvents = new AutoResetEvent[]
        {
            new AutoResetEvent(false),
            new AutoResetEvent(false)
        };

        internal Action<PoolCommand> ReceivedCommandAction;
        internal Action<bool> ConnectStatusChangedAction;
        internal Action<int> ProcessErrorAction;

        // Create an uninitialized client instance.
        // To start the send/receive processing call the
        // Connect method followed by SendReceive method.
        internal SocketClient(String hostName, Int32 port)
        {
            // Get host related information.
            IPHostEntry host = Dns.GetHostEntry(hostName);

            // Address of the host.
            IPAddress[] addressList = host.AddressList;

            // Instantiates the endpoint and socket.
            HostEndPoint =
              new IPEndPoint(addressList[addressList.Length - 1], port);
            ClientSocket = new Socket(HostEndPoint.AddressFamily,
                               SocketType.Stream, ProtocolType.Tcp);
        }

        // Connect to the host.
        internal void Connect()
        {
            SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();

            connectArgs.UserToken = ClientSocket;
            connectArgs.RemoteEndPoint = HostEndPoint;
            connectArgs.Completed +=
               new EventHandler<SocketAsyncEventArgs>(OnConnect);

            ClientSocket.ConnectAsync(connectArgs);
            //autoConnectEvent.WaitOne();

            SocketError errorCode = connectArgs.SocketError;
            if (errorCode != SocketError.Success)
            {
                if (ProcessErrorAction != null)
                {
                    ProcessErrorAction((Int32)errorCode);
                }
                //throw new SocketException((Int32)errorCode);
            }

            Console.ReadLine();
        }

        /// Disconnect from the host.
        internal void Disconnect()
        {
            ClientSocket.Disconnect(false);
        }

        internal void StartReceive()
        {
            if (connected)
            {
                if (this.sendArgs == null)
                {
                    receiveArgs = new SocketAsyncEventArgs();
                    receiveArgs.UserToken = ClientSocket;
                    receiveArgs.RemoteEndPoint = HostEndPoint;
                    receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceive);
                }


                byte[] receiveBuffer = new byte[Int16.MaxValue];
                receiveArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
                ClientSocket.ReceiveAsync(receiveArgs);
                //autoSendReceiveEvents[ReceiveOperation].WaitOne();
            }
            else
            {
                if (ProcessErrorAction != null)
                {
                    ProcessErrorAction((Int32)SocketError.NotConnected);
                }
                //throw new SocketException((Int32)SocketError.NotConnected);
            }
        }

        internal void SendCommand(PoolCommand cmd)
        {
            if (connected)
            {
                // Create a buffer to send.
                var sendBuffer = cmd.GetBytes();

                if (this.sendArgs == null)
                {
                    sendArgs = new SocketAsyncEventArgs();
                    sendArgs.UserToken = ClientSocket;
                    sendArgs.RemoteEndPoint = HostEndPoint;
                    sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);
                }

                // Start sending asynchronously.
                sendArgs.SetBuffer(sendBuffer, 0, sendBuffer.Length);
                ClientSocket.SendAsync(sendArgs);

                // Wait for the send/receive completed.
                //autoSendReceiveEvents[SendOperation].WaitOne();
            }
            else
            {
                if (ProcessErrorAction != null)
                {
                    ProcessErrorAction((Int32)SocketError.NotConnected);
                }
                //throw new SocketException((Int32)SocketError.NotConnected);
            }
        }

       // Calback for connect operation
        private void OnConnect(object sender, SocketAsyncEventArgs e)
        {
            // Signals the end of connection.
            //autoConnectEvent.Set();

            // Set the flag for socket connected.
            connected = (e.SocketError == SocketError.Success);

            if(connected)
            {
                LogHelper.Info("Connected, Local EP: " + this.ClientSocket.LocalEndPoint.ToString());
                StartReceive();
            }

            if(ConnectStatusChangedAction != null)
            {
                ConnectStatusChangedAction(connected);
            }
        }

        // Calback for receive operation
        private void OnReceive(object sender, SocketAsyncEventArgs e)
        {
            // Signals the end of receive.
           // autoSendReceiveEvents[SendOperation].Set();

            if (e.SocketError == SocketError.Success)
            {
                var buffer = new byte[e.BytesTransferred];
                Array.Copy(e.Buffer, e.Offset, buffer, 0, buffer.Length);

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

                foreach (var data in commandDataList)
                {
                    if(ReceivedCommandAction != null)
                    {
                        var cmd = PoolCommand.ConvertBytesToMessage(data);
                        ReceivedCommandAction(cmd);
                    }
                }

                this.StartReceive();
            }
            else
            {
                ProcessError(e);
            }
        }

        // Calback for send operation
        private void OnSend(object sender, SocketAsyncEventArgs e)
        {
            // Signals the end of send.
            //autoSendReceiveEvents[ReceiveOperation].Set();

            if (e.SocketError == SocketError.Success)
            {
            }
            else
            {
                ProcessError(e);
            }
        }

        // Close socket in case of failure and throws
        // a SockeException according to the SocketError.
        private void ProcessError(SocketAsyncEventArgs e)
        {
            Socket s = e.UserToken as Socket;
            if (s.Connected)
            {
                // close the socket associated with the client
                try
                {
                    s.Shutdown(SocketShutdown.Both);
                }
                catch (Exception)
                {
                    // throws if client process has already closed
                }
                finally
                {
                    if (s.Connected)
                    {
                        s.Close();
                    }
                }
            }

            // Throw the SocketException
            //throw new SocketException((Int32)e.SocketError);

            if(ProcessErrorAction != null)
            {
                ProcessErrorAction((Int32)e.SocketError);
            }
        }

        // Exchange a message with the host.

        #region IDisposable Members

        // Disposes the instance of SocketClient.
        public void Dispose()
        {
            autoConnectEvent.Close();
            autoSendReceiveEvents[SendOperation].Close();
            autoSendReceiveEvents[ReceiveOperation].Close();
            if (ClientSocket.Connected)
            {
                ClientSocket.Close();
            }
        }

        #endregion
    }
}
