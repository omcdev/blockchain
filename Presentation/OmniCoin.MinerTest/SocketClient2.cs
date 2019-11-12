

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using OmniCoin.PoolMessages;
using OmniCoin.Framework;

namespace OmniCoin.MinerTest
{
    class SocketClient2
    {
        TcpClient client;
        NetworkStream stream;
        IPEndPoint serverEP;
        bool isConnected;
        int m_receiveBufferSize;

        internal Action<PoolCommand> ReceivedCommandAction;
        internal Action<bool> ConnectStatusChangedAction;
        internal Action<int> ProcessErrorAction;

        public SocketClient2(IPEndPoint ep, int receiveBufferSize)
        {
            serverEP = ep;
            m_receiveBufferSize = receiveBufferSize;
            this.client = new TcpClient(AddressFamily.InterNetwork);
        }

        public void Connect()
        {
            this.client.ReceiveTimeout = 1000 * 60 * 30;
            this.client.SendTimeout = 1000 * 60 * 30;
            this.client.BeginConnect(serverEP.Address.ToString(), serverEP.Port, new AsyncCallback(processConnect), client);
        }
        public void Disconnect()
        {
            this.isConnected = false;

            try
            {
                this.stream.Close();
            }
            catch
            {

            }

            try
            { 
                this.client.Close();
            }
            catch
            {

            }

            if(this.ConnectStatusChangedAction != null)
            {
                this.ConnectStatusChangedAction(false);
            }
        }

        private void processConnect(IAsyncResult ar)
        {
            try
            {
                this.client.EndConnect(ar);
                this.isConnected = true;
                this.stream = this.client.GetStream();
                var buffer = new byte[m_receiveBufferSize];
                stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(processReceive), buffer);

                if (this.ConnectStatusChangedAction != null)
                {
                    this.ConnectStatusChangedAction(true);
                }
            }
            catch(Exception ex)
            {
                LogHelper.Error(ex.ToString());
                if (this.ConnectStatusChangedAction != null)
                {
                    this.ConnectStatusChangedAction(false);
                }
            }
        }

        public void SendCommand(PoolCommand command)
        {
            try
            {
                var buffer = command.GetBytes();
                stream.BeginWrite(buffer, 0, buffer.Length, new AsyncCallback(processSend), stream);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
                this.Disconnect();
            }
        }
        private void processSend(IAsyncResult ar)
        {
            var stream = (NetworkStream)ar.AsyncState;

            try
            {
                stream.EndWrite(ar);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
                this.Disconnect();
            }
        }
        private void processReceive(IAsyncResult ar)
        {
            var sourceBuffer = (byte[])ar.AsyncState;

            try
            {
                int numberOfBytesRead = this.stream.EndRead(ar);

                if (numberOfBytesRead > 0)
                {
                    var buffer = new byte[numberOfBytesRead];
                    Array.Copy(sourceBuffer, 0, buffer, 0, buffer.Length);

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
                                this.ReceivedCommandAction(cmd);
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Error("Error occured on deserialize messgae: " + ex.Message, ex);
                            }
                        }
                    }

                    sourceBuffer = new byte[m_receiveBufferSize];
                    this.stream.BeginRead(sourceBuffer, 0, sourceBuffer.Length,
                        new AsyncCallback(processReceive), sourceBuffer);
                }
                else
                {
                    this.stream.Close();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("Error occured on receive messgae: " + ex.Message, ex);
                this.Disconnect();
            }
        }
    }
}
