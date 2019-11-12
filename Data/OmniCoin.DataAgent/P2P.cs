

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Entities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.Timers;
using OmniCoin.Framework;
using OmniCoin.Messages;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace OmniCoin.DataAgent
{
    public class P2P
    {
        public static P2P Instance;
        public List<P2PNode> Peers;
        public List<string> BlackList;
        public long TotalBytesReceived = 0;
        public long TotalBytesSent = 0;
        public bool IsRunning = false;
        public string Identity = "";
        public int InboundLimit = 0;
        public int OutboundLimit = 0;

        public long LastBlockHeight { get; set; }
        public long LastBlockTime { get; set; }

        private UdpClient server;
        private int bufferSize = 1 * 1024;
        private int maxConnections = 60;
        private int p2pSendSleep = 60;
        private string defaultTrackerIP = "47.244.105.187";
        private ConcurrentQueue<P2PSendMessage> sendCommandQueue;
        private ConcurrentQueue<ReceivedDataQueueItem> receivedDataQueue;
        private Dictionary<string, List<byte>> receivedMessageBuffer;
        private SafeCollection<P2PSendMessage> sendCommandCache;
        private SafeCollection<ReceivedDataPacketItem> receivedCommandCache;
        private System.Timers.Timer peerCheckTimer;
        private System.Timers.Timer handleReceivedDataTimer;
        private System.Timers.Timer packetCheckTimer;
        private byte[] defaultPrefixBytes, defaultSuffixByste;
        private bool isTrackerNode = false;
        private byte[] prevBuffer = null;
        private IAsyncResult currentAynchResult = null;
        private string localIPText = "";
        private int localPort = 0;

        static P2P()
        {
            Instance = new P2P();
        }
        public P2P()
        {
            LastBlockHeight = -1;
            LastBlockTime = 0;

            this.InboundLimit = this.maxConnections / 2;
            this.OutboundLimit = this.maxConnections - this.InboundLimit;

            this.Peers = new List<P2PNode>();
            peerCheckTimer = new System.Timers.Timer(2 * 60 * 1000); //30 seconds
            peerCheckTimer.AutoReset = true;
            peerCheckTimer.Elapsed += peerCheckTimer_Elapsed;

            handleReceivedDataTimer = new System.Timers.Timer(1);
            handleReceivedDataTimer.AutoReset = true;
            handleReceivedDataTimer.Elapsed += HandleReceivedDataTimer_Elapsed;

            packetCheckTimer = new System.Timers.Timer(30 * 1000);
            packetCheckTimer.AutoReset = true;
            packetCheckTimer.Elapsed += PacketCheckTimer_Elapsed;

            defaultPrefixBytes = P2PCommand.DefaultPrefixBytes;
            defaultSuffixByste = P2PCommand.DefaultSuffixBytes;
        }

        private void PacketCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var currentTime = Time.EpochTime;
            var sendItems = this.sendCommandCache.Where(p => (currentTime - p.Timestamp) >= 3 * 60 * 1000).ToList();

            foreach (var item in sendItems)
            {
                //LogHelper.Debug($"Remove sendItems {item.Id} from {item.Address}:{item.Port}");
                this.sendCommandCache.Remove(item);
            }

            var receivedItems = this.receivedCommandCache.Where(p => (currentTime - p.Timestamp) >= 2 * 60 * 1000).ToList();

            foreach (var item in receivedItems)
            {
                //LogHelper.Debug($"Remove receivedItems {item.Id} from {item.IP}:{item.Port}");
                this.receivedCommandCache.Remove(item);
            }

            receivedItems = this.receivedCommandCache.Where(p => !p.IsRequestResend && /*(p.CommandName == CommandNames.Block.Blocks || p.CommandName == CommandNames.Transaction.Tx) &&*/ (currentTime - p.Timestamp) >= 30 * 1000 && (p.Count > p.Packets.Count)).ToList();


            foreach (var item in receivedItems)
            {
                for (int i = 0; i < item.Count; i++)
                {
                    if (!item.Packets.ContainsKey(i))
                    {
                        item.IsRequestResend = true;
                        PktLostMsg msg = new PktLostMsg();
                        msg.Id = item.Id;
                        msg.Index = i;

                        var cmd = P2PCommand.CreateCommand(this.Identity, CommandNames.Packet.PktLost, msg);
                        this.Send(cmd, item.IP, item.Port);
                        //item.Timestamp = Time.EpochTime;
                        LogHelper.Debug($"Resend receivedItems {msg.Index + 1}/{item.Count},{item.Id} from {item.IP}:{item.Port}");
                    }
                }
            }
        }

        private void HandleReceivedDataTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.handleReceivedDataTimer.Stop();

            try
            {
                if (this.receivedDataQueue.Count == 0)
                {
                    return;
                }

                ReceivedDataQueueItem item = null;
                if (!receivedDataQueue.TryDequeue(out item) || item == null || item.Data == null)
                {
                    return;
                }

                //if (item == null || item.Data == null)
                //{
                //    return;
                //}

                var buffer = item.Data;

                var peer = this.Peers.Where(p => p.IP == item.IP && p.Port == item.Port).FirstOrDefault();

                if (peer != null)
                {
                    peer.TotalBytesReceived += buffer.Length;
                    peer.LastReceivedTime = Time.EpochTime;
                }

                LogHelper.Debug("Received cmd from " + item.IP + ":" + item.Port + ", Data:" + Base16.Encode(buffer));

                var prefix = new byte[4];
                var suffix = new byte[4];
                bool isBufferEnd = false;
                var key = item.IP + ":" + item.Port;

                if (buffer.Length > 4)
                {
                    Array.Copy(buffer, 0, prefix, 0, 4);
                    Array.Copy(buffer, buffer.Length - 4, suffix, 0, 4);

                    if (!this.receivedMessageBuffer.ContainsKey(key))
                    {
                        this.receivedMessageBuffer.Add(key, new List<byte>());
                    }

                    //first data package
                    if (P2PCommand.BytesEquals(P2PCommand.DefaultPrefixBytes, prefix))
                    {
                        this.receivedMessageBuffer[key] = new List<byte>();
                        this.receivedMessageBuffer[key].AddRange(buffer);

                        //last data package
                        if (P2PCommand.BytesEquals(P2PCommand.DefaultSuffixBytes, suffix))
                        {
                            isBufferEnd = true;
                        }
                    }
                    else if (P2PCommand.BytesEquals(P2PCommand.DefaultSuffixBytes, suffix))
                    {
                        this.receivedMessageBuffer[key].AddRange(buffer);
                        isBufferEnd = true;
                    }
                    //other data package
                    else
                    {
                        this.receivedMessageBuffer[key].AddRange(buffer);
                    }
                }
                else
                {
                    this.receivedMessageBuffer[key].AddRange(buffer);
                    isBufferEnd = true;
                }

                if (isBufferEnd)
                {
                    var command = P2PCommand.ConvertBytesToMessage(this.receivedMessageBuffer[key].ToArray());

                    if (command == null || command.Identity == this.Identity)
                    {
                        return;
                    }

                    P2PState state = new P2PState();
                    state.IP = item.IP;
                    state.Port = item.Port;
                    state.Command = command;

                    if (command != null)
                    {
                        LogHelper.Debug("Received cmd from " + item.IP + ":" + item.Port + ", Command:" + command.CommandName);

                        if (peer == null && command.CommandName != CommandNames.P2P.Ping)
                        {
                            this.ConnectToNewPeer(item.IP, item.Port);
                            return;
                        }
                        else if (peer != null)
                        {
                            peer.Identity = command.Identity;
                        }

                        switch (command.CommandName)
                        {
                            case CommandNames.P2P.Ping:
                                this.pingMsgHandle(state);
                                break;
                            case CommandNames.P2P.Pong:
                                this.pongMsgHandle(state);
                                break;
                            case CommandNames.P2P.Version:
                                this.versionMsgHandle(state);
                                break;
                            case CommandNames.P2P.VerAck:
                                this.verAckMsgHandle(state);
                                break;
                            case CommandNames.P2P.GetAddr:
                                this.getAddrMsgHandle(state);
                                break;
                            case CommandNames.P2P.Addr:
                                this.addrMsgHandle(state);
                                break;
                            case CommandNames.P2P.Heartbeat:
                                this.heartbeatMsgHandle(state);
                                break;
                            case CommandNames.Other.Reject:
                                this.rejectMsgHandle(state);
                                break;
                            default:
                                raiseDataReceived(state);
                                break;
                        }
                    }
                }

                if (peer != null)
                {
                    var commandName = P2PCommand.GetCommandName(buffer);

                    if (commandName == null)
                    {
                        return;
                    }
                    else
                    {
                        peer.LastCommand = commandName;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!(ex is SocketException))
                {
                    LogHelper.Warn(ex.ToString());
                }
                else
                {
                    LogHelper.Error(ex.Message, ex);
                }
                raiseOtherException(null);
            }
            finally
            {
                if (this.IsRunning)
                {
                    try
                    {
                        handleReceivedDataTimer.Start();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error(ex.Message, ex);
                        handleReceivedDataTimer.Start();
                    }
                }
            }
        }

        private void peerCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            for (int i = Peers.Count - 1; i >= 0; i--)
            {
                var peer = Peers[i];
                var nowTime = Time.EpochTime;

                //check unconnected peers
                if (!peer.IsConnected)
                {
                    if ((peer.ConnectedTime > nowTime || (nowTime - peer.ConnectedTime) > 3 * 60 * 1000))
                    {
                        Peers.RemoveAt(i);
                        this.receivedMessageBuffer.Remove(peer.IP + ":" + peer.Port);
                    }
                }
                else
                {
                    //check long time not received heartbeat peers
                    if ((nowTime - peer.LastHeartbeat) > 10 * 60 * 1000 && (nowTime - peer.LastReceivedTime) > 10 * 60 * 1000)
                    {
                        peer.IsConnected = false;
                        this.raiseNodeConnectionStateChanged(peer);

                        Peers.RemoveAt(i);
                        this.receivedMessageBuffer.Remove(peer.IP + ":" + peer.Port);
                    }
                    else if (this.isTrackerNode)
                    {
                        var msg = new HeightMsg();
                        msg.Height = this.LastBlockHeight;
                        msg.BlockTime = this.LastBlockTime;
                        var command = P2PCommand.CreateCommand(this.Identity, CommandNames.P2P.Heartbeat, msg);
                        this.Send(command, peer.IP, peer.Port);
                    }
                }
            }

            if (!this.isTrackerNode)
            {
                //reconnect with tracker nodes
                if (this.Peers.Count == 0 || this.Peers.Where(p => p.IsTrackerServer && p.IsConnected).Count() == 0)
                {
                    var trackerNode = this.Peers.Where(t => t.IsTrackerServer).FirstOrDefault();

                    if (trackerNode != null)
                    {
                        this.Peers.Remove(trackerNode);
                        this.receivedMessageBuffer.Remove(trackerNode.IP + ":" + trackerNode.Port);
                    }

                    string trackerIp = GlobalParameters.IsTestnet ? ConfigCenter.ConfigNode.TestnetTrackerServer : ConfigCenter.ConfigNode.MainnetTrackerServer;
                    int trackerPort = GlobalParameters.IsTestnet ? ConfigCenter.ConfigNode.TestnetTrackerPort : ConfigCenter.ConfigNode.MainnetTrackerPort;
                    this.ConnectToNewPeer(trackerIp, trackerPort, true);
                }
                //get new node address from tracker
                else if (this.Peers.Count() < this.maxConnections)
                {
                    var tracker = this.Peers.Where(p => p.IsTrackerServer && p.IsConnected).FirstOrDefault();

                    if (tracker != null)
                    {
                        var payload = new GetAddrMsg();
                        payload.Count = this.OutboundLimit;

                        var command = P2PCommand.CreateCommand(this.Identity, CommandNames.P2P.GetAddr, payload);
                        this.Send(command, tracker.IP, tracker.Port);
                    }
                }
            }
        }

        public Action<P2PState> DataReceived;
        public Action<P2PState> OtherException;
        public Action<P2PState> PrepareSend;
        public Action<P2PState> CompletedSend;
        public Action<P2PNode> NodeConnectionStateChanged;

        public void Start(Guid guid, string ipText,int port, bool isTracker)
        {
            this.localIPText = ipText;
            this.localPort = port;
            this.isTrackerNode = isTracker;
            this.Identity = guid.ToString();

            TotalBytesReceived = 0;
            TotalBytesSent = 0;

            IPAddress ipAddress = IPAddress.Any;

            if (!string.IsNullOrWhiteSpace(ipText))
            {
                ipAddress = IPAddress.Parse(ipText);
            }

            IPEndPoint ip = new IPEndPoint(ipAddress, port);            

            this.server = new UdpClient(ip);

            this.Peers = new List<P2PNode>();
            this.sendCommandQueue = new ConcurrentQueue<P2PSendMessage>();
            this.sendCommandCache = new SafeCollection<P2PSendMessage>();
            this.receivedDataQueue = new ConcurrentQueue<ReceivedDataQueueItem>();
            this.receivedCommandCache = new SafeCollection<ReceivedDataPacketItem>();
            this.receivedMessageBuffer = new Dictionary<string, List<byte>>();
            this.BlackList = new List<string>();

            this.IsRunning = true;
            this.server.Client.SendBufferSize = bufferSize;
            this.server.Client.ReceiveBufferSize = bufferSize;
            currentAynchResult = this.server.BeginReceive(receiveDataAsync, null);
            this.peerCheckTimer.Start();
            this.packetCheckTimer.Start();

            if (!this.isTrackerNode)
            {
                string trackerIp = GlobalParameters.IsTestnet ? ConfigCenter.ConfigNode.TestnetTrackerServer : ConfigCenter.ConfigNode.MainnetTrackerServer;
                int trackerPort = GlobalParameters.IsTestnet ? ConfigCenter.ConfigNode.TestnetTrackerPort : ConfigCenter.ConfigNode.MainnetTrackerPort;
                this.ConnectToNewPeer(trackerIp, trackerPort, true);
            }

            //handleReceivedDataTimer.Start();
            Task.Run(() => {
                startHandleReceivedData();
            });

            this.startSendCommand();
        }
        public void Stop()
        {
            this.peerCheckTimer.Stop();
            this.packetCheckTimer.Stop();
            handleReceivedDataTimer.Stop();

            if (this.IsRunning)
            {
                this.IsRunning = false;
                this.server.Close();
            }
        }
        public void Send(P2PCommand command, string address, int port)
        {
            this.sendCommandQueue.Enqueue(new P2PSendMessage
            {
                Id = Base16.Encode(command.Checksum),
                Address = address,
                Port = port,
                Command = command,
                Timestamp = Time.EpochTime
            });
        }
        public bool ConnectToNewPeer(string address, int port, bool isTracker = false)
        {
            IPAddress ip;
            if (!IPAddress.TryParse(address, out ip))
            {
                try
                {
                    var ips = Dns.GetHostAddresses(address);

                    if (ips.Length > 0)
                    {
                        ip = ips[0];
                        address = ip.ToString();
                    }
                    else
                    {
                        LogHelper.Warn(string.Format("Cannot parse host {0} to ip, use default ip address", address));
                        address = defaultTrackerIP;
                        //throw new CommonException(ErrorCode.Engine.P2P.Connection.HOST_NAME_CAN_NOT_RESOLVED_TO_IP_ADDRESS);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error(string.Format("Cannot parse host {0} to ip, use default ip address", address), ex);
                    address = defaultTrackerIP;
                    //throw new CommonException(ErrorCode.Engine.P2P.Connection.HOST_NAME_CAN_NOT_RESOLVED_TO_IP_ADDRESS);
                }
            }

            if (BlackList.Contains(address + ":" + port))
            {
                throw new CommonException(ErrorCode.Engine.P2P.Connection.PEER_IN_BLACK_LIST);
            }

            var peer = this.Peers.Where(p => p.IP == address && p.Port == port).FirstOrDefault();
            if (peer != null)
            {
                //if (!this.receivedMessageBuffer.ContainsKey(address + ":" + port))
                //{
                //    this.receivedMessageBuffer.Add(address + ":" + port, new List<byte>());
                //}
                ////else
                ////{
                ////    this.receivedMessageBuffer[address + ":" + port] = new List<byte>();
                ////}

                ////var command = P2PCommand.CreateCommand(CommandNames.P2P.Ping, null);
                ////this.Send(command, peer.IP, peer.Port);

                //LogHelper.Debug(" Connect to " + address);
                ////return true;
                //throw new CommonException(ErrorCode.Engine.P2P.Connection.THE_PEER_IS_EXISTED);

                return false;
            }
            else
            {
                if (!this.isTrackerNode && Peers.Count >= this.maxConnections)
                {
                    throw new CommonException(ErrorCode.Engine.P2P.Connection.THE_NUMBER_OF_CONNECTIONS_IS_FULL);
                }

                peer = new P2PNode();
                peer.IP = address;
                peer.Port = port;
                peer.Identity = Guid.Empty.ToString();
                peer.IsConnected = false;
                peer.ConnectedTime = Time.EpochTime;
                peer.IsTrackerServer = isTracker;
                peer.IsInbound = false;

                this.Peers.Add(peer);
                this.receivedMessageBuffer[address + ":" + port] = new List<byte>();

                var command = P2PCommand.CreateCommand(this.Identity, CommandNames.P2P.Ping, null);
                this.Send(command, peer.IP, peer.Port);

                LogHelper.Debug(" Connect to " + address);
                return true;
            }
        }
        public bool RemovePeer(string address, int port)
        {
            var peer = this.Peers.Where(p => p.IP == address && p.Port == port).FirstOrDefault();

            if (peer != null)
            {
                this.Peers.Remove(peer);
                this.receivedMessageBuffer.Remove(peer.IP + ":" + peer.Port);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void startSendCommand()
        {
            while (this.server != null)
            {
                if (!this.sendCommandQueue.IsEmpty)
                {
                    try
                    {
                        P2PSendMessage item = null;

                        if (this.sendCommandQueue.TryDequeue(out item))
                        {
                            if (item != null && item.Command != null)
                            {
                                raisePrepareSend(null);

                                var index = 0;
                                var data = item.Command.GetBytes();
                                var packetsCount = data.Length % this.bufferSize > 0 ? data.Length / this.bufferSize + 1 : data.Length / this.bufferSize;
                                var packetIndex = 0;
                                var peerVersion = 1;
                                var peer = this.Peers.FirstOrDefault(p => p.IP == item.Address && p.Port == item.Port);

                                if (peer != null && peer.Version > 1)
                                {
                                    peerVersion = peer.Version;

                                    if (packetsCount > 1)
                                    {
                                        item.Timestamp = Time.EpochTime;
                                        if (this.sendCommandCache.Any(c => c.Address == item.Address && c.Port == item.Port && c.Id == item.Id))
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            //LogHelper.Debug($"Added new sendItems {item.Id} into cache");
                                            this.sendCommandCache.Add(item);
                                        }
                                    }
                                }

                                while (index < data.Length)
                                {
                                    byte[] buffer;
                                    List<byte> packetBytes = new List<byte>();

                                    if (peerVersion >= 2/* && packetsCount > 1*/)
                                    {
                                        var itemIndexBytes = BitConverter.GetBytes(packetIndex);
                                        var packetsCountBytes = BitConverter.GetBytes(packetsCount);

                                        if (BitConverter.IsLittleEndian)
                                        {
                                            Array.Reverse(itemIndexBytes);
                                            Array.Reverse(packetsCountBytes);
                                        }

                                        packetBytes.AddRange(P2PPacket.DefaultPacketPrefixBytes);
                                        packetBytes.AddRange(item.Command.Checksum);
                                        packetBytes.AddRange(itemIndexBytes);
                                        packetBytes.AddRange(packetsCountBytes);

                                        packetIndex++;
                                    }


                                    if (data.Length > index + this.bufferSize)
                                    {
                                        buffer = new byte[this.bufferSize];
                                    }
                                    else
                                    {
                                        buffer = new byte[data.Length - index];
                                    }

                                    Array.Copy(data, index, buffer, 0, buffer.Length);
                                    var fullBuffer = new List<byte>();
                                    fullBuffer.AddRange(packetBytes);
                                    fullBuffer.AddRange(buffer);

                                    this.server.BeginSend(fullBuffer.ToArray(), fullBuffer.Count, item.Address, item.Port, this.sendCallback, null);

                                    this.TotalBytesSent += buffer.Length;
                                    //var peer = this.Peers.Where(p => p.IP == item.Address && p.Port == item.Port).FirstOrDefault();

                                    if (peer != null)
                                    {
                                        peer.TotalBytesSent += buffer.Length;
                                        peer.LastSentTime = Time.EpochTime;
                                    }

                                    index += buffer.Length;
                                    Thread.Sleep(p2pSendSleep);
                                }

                                LogHelper.Debug("Send " + item.Command.CommandName + " to " + item.Address + ":" + item.Port);
                            }
                            else if (item.Packet != null)
                            {
                                var peer = this.Peers.FirstOrDefault(p => p.IP == item.Address && p.Port == item.Port);

                                if (peer != null && peer.Version > 1)
                                {
                                    var buffer = item.Packet.Serialize();
                                    this.server.BeginSend(buffer, buffer.Length, item.Address, item.Port, this.sendCallback, null);
                                    this.TotalBytesSent += buffer.Length;
                                    peer.TotalBytesSent += buffer.Length;
                                    peer.LastSentTime = Time.EpochTime;

                                    LogHelper.Debug($"Resend command {item.Packet.Id} {item.Packet.Index}/{item.Packet.Count} to {item.Address}:{item.Port}");
                                    Thread.Sleep(p2pSendSleep);
                                }
                            }
                        }

                    }
                    catch (Exception)
                    {
                        raiseOtherException(null);
                    }
                }
                else
                {
                    Thread.Sleep(p2pSendSleep);
                }

                if (!this.IsRunning)
                {
                    break;
                }
            }
        }
        private void startHandleReceivedData()
        {
            while (this.server != null && this.IsRunning)
            {
                try
                {
                    if (this.receivedDataQueue.IsEmpty)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    ReceivedDataQueueItem item = null;

                    if (!receivedDataQueue.TryDequeue(out item) || item == null || item.Data == null)
                    {
                        continue;
                    }

                    var buffer = item.Data;

                    var peer = this.Peers.Where(p => p.IP == item.IP && p.Port == item.Port).FirstOrDefault();

                    if (peer != null)
                    {
                        peer.TotalBytesReceived += buffer.Length;
                        peer.LastReceivedTime = Time.EpochTime;
                    }

                    var prefix = new byte[4];
                    var suffix = new byte[4];
                    bool isBufferEnd = false;
                    bool isOneBuffer = false;
                    var key = item.IP + ":" + item.Port;

                    if (buffer.Length > 4)
                    {
                        Array.Copy(buffer, 0, prefix, 0, 4);
                        Array.Copy(buffer, buffer.Length - 4, suffix, 0, 4);

                        if (!this.receivedMessageBuffer.ContainsKey(key))
                        {
                            this.receivedMessageBuffer.Add(key, new List<byte>());
                        }

                        //first data package
                        if (P2PCommand.BytesEquals(P2PCommand.DefaultPrefixBytes, prefix))
                        {
                            //last data package
                            if (P2PCommand.BytesEquals(P2PCommand.DefaultSuffixBytes, suffix))
                            {
                                isBufferEnd = true;
                                isOneBuffer = true;
                            }
                            else
                            {
                                this.receivedMessageBuffer[key] = new List<byte>();
                                this.receivedMessageBuffer[key].AddRange(buffer);
                            }
                        }
                        //version 2: message packet handler
                        else if (P2PPacket.BytesEquals(P2PPacket.DefaultPacketPrefixBytes, prefix))
                        {
                            var packet = P2PPacket.Deserialize(buffer);
                            if (packet.Count > 1)
                            {
                                LogHelper.Debug($"Packet From {item.IP}:{item.Port}, id:{packet.Id}, index/count:{packet.Index + 1}/{packet.Count}");
                            }

                            if (peer != null && packet.Index == 0)
                            {
                                var lastCommand = packet.GetCommandName();

                                if (!string.IsNullOrWhiteSpace(lastCommand))
                                {
                                    peer.LastCommand = lastCommand;
                                }

                                //if remote node update to new verion, change version in node list
                                if (peer.Version < Versions.EngineVersion)
                                {
                                    peer.Version = Versions.EngineVersion;
                                }
                            }

                            var packetItem = this.receivedCommandCache.FirstOrDefault(p => p.Id == packet.Id && p.IP == item.IP && p.Port == item.Port);

                            if (packetItem == null)
                            {
                                if (packet.Index == 0)
                                {
                                    packetItem = new ReceivedDataPacketItem();
                                    packetItem.IP = item.IP;
                                    packetItem.Port = item.Port;
                                    packetItem.Id = packet.Id;
                                    packetItem.Count = packet.Count;
                                    packetItem.Timestamp = Time.EpochTime;
                                    packetItem.CommandName = packet.GetCommandName();
                                    packetItem.Packets.Add(packet.Index, packet);

                                    receivedCommandCache.Add(packetItem);
                                }
                            }
                            else
                            {
                                if (!packetItem.Packets.ContainsKey(packet.Index))
                                {
                                    packetItem.Packets.Add(packet.Index, packet);
                                    packetItem.Timestamp = Time.EpochTime;
                                }
                            }

                            if (packetItem != null && packetItem.Packets.Count == packetItem.Count)
                            {
                                var data = new List<byte>();

                                foreach (var p in packetItem.Packets)
                                {
                                    data.AddRange(p.Value.Data);
                                }

                                buffer = data.ToArray();
                                isBufferEnd = true;
                                isOneBuffer = true;
                                this.receivedCommandCache.Remove(packetItem);

                                if (packetItem.Count > 1)
                                {
                                    var pktFinishedMsg = new PktFinishedMsg();
                                    pktFinishedMsg.Id = packetItem.Id;

                                    var cmd = P2PCommand.CreateCommand(this.Identity, CommandNames.Packet.PktFinished, pktFinishedMsg);
                                    this.Send(cmd, packetItem.IP, packetItem.Port);
                                }
                            }
                        }
                        else if (P2PCommand.BytesEquals(P2PCommand.DefaultSuffixBytes, suffix))
                        {
                            this.receivedMessageBuffer[key].AddRange(buffer);
                            isBufferEnd = true;
                        }
                        //other data package
                        else
                        {
                            this.receivedMessageBuffer[key].AddRange(buffer);
                        }
                    }
                    else
                    {
                        if (this.receivedMessageBuffer.ContainsKey(key) && this.receivedMessageBuffer[key] != null)
                        {
                            this.receivedMessageBuffer[key].AddRange(buffer);
                        }

                        isBufferEnd = true;
                    }

                    if (isBufferEnd)
                    {
                        if (!isOneBuffer)
                        {
                            buffer = this.receivedMessageBuffer[key].ToArray();
                        }

                        var command = P2PCommand.ConvertBytesToMessage(buffer);

                        if (command == null || command.Identity == this.Identity)
                        {
                            continue;
                        }

                        P2PState state = new P2PState();
                        state.IP = item.IP;
                        state.Port = item.Port;
                        state.Command = command;

                        if (command != null)
                        {
                            LogHelper.Debug("Received cmd from " + item.IP + ":" + item.Port + ", Command:" + command.CommandName);
                            LogHelper.Debug($"Current queue size {this.receivedDataQueue.Count}");

                            if (peer == null && command.CommandName != CommandNames.P2P.Ping)
                            {
                                this.ConnectToNewPeer(item.IP, item.Port);
                                continue;
                            }
                            else if (peer != null)
                            {
                                peer.Identity = command.Identity;
                                peer.LastCommand = command.CommandName;
                            }
                            //System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                            //stopwatch.Start();
                            switch (command.CommandName)
                            {
                                case CommandNames.P2P.Ping:
                                    this.pingMsgHandle(state);
                                    break;
                                case CommandNames.P2P.Pong:
                                    this.pongMsgHandle(state);
                                    break;
                                case CommandNames.P2P.Version:
                                    this.versionMsgHandle(state);
                                    break;
                                case CommandNames.P2P.VerAck:
                                    this.verAckMsgHandle(state);
                                    break;
                                case CommandNames.P2P.GetAddr:
                                    this.getAddrMsgHandle(state);
                                    break;
                                case CommandNames.P2P.Addr:
                                    this.addrMsgHandle(state);
                                    break;
                                case CommandNames.P2P.Heartbeat:
                                    this.heartbeatMsgHandle(state);
                                    break;
                                case CommandNames.Other.Reject:
                                    this.rejectMsgHandle(state);
                                    break;
                                case CommandNames.Packet.PktLost:
                                    this.pktLostMsgHandle(state);
                                    break;
                                case CommandNames.Packet.PktFinished:
                                    this.pktFinishedMsgHandle(state);
                                    break;
                                default:
                                    raiseDataReceived(state);
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!(ex is SocketException))
                    {
                        LogHelper.Warn(ex.ToString());
                    }
                    else
                    {
                        LogHelper.Error(ex.Message, ex);
                    }
                }
                finally
                {
                }
            }
        }
        private void packetCheck(ReceivedDataPacketItem item)
        {
            for (int i = 0; i < item.Count; i++)
            {
                if (!item.Packets.ContainsKey(i))
                {
                    PktLostMsg msg = new PktLostMsg();
                    msg.Id = item.Id;
                    msg.Index = i;

                    var cmd = P2PCommand.CreateCommand(this.Identity, CommandNames.Packet.PktLost, msg);
                    this.Send(cmd, item.IP, item.Port);
                    item.Timestamp = Time.EpochTime;
                    LogHelper.Debug($"Resend receivedItems {msg.Index}/{item.Count},{item.Id} from {item.IP}:{item.Port}");
                }
            }
        }
        private void sendCallback(IAsyncResult ar)
        {
            if (ar.IsCompleted)
            {
                try
                {
                    this.server.EndSend(ar);
                    raiseCompletedSend(null);
                }
                catch (Exception)
                {
                    raiseOtherException(null);
                }
            }

        }
        private void raisePrepareSend(P2PState state)
        {
            if (PrepareSend != null)
            {
                PrepareSend(state);
            }
        }
        private void raiseCompletedSend(P2PState state)
        {
            if (CompletedSend != null)
            {
                CompletedSend(state);
            }
        }
        private void receiveDataAsync(IAsyncResult ar)
        {
            IPEndPoint remote = null;
            byte[] buffer = null;

            try
            {
                
                if (server != null && this.IsRunning && ar != null)
                {
                    buffer = server.EndReceive(ar, ref remote);
                    Console.WriteLine($"recive {remote.ToString()}");
                    this.TotalBytesReceived += buffer.Length;

                    if (prevBuffer == null)
                    {
                        prevBuffer = buffer;
                    }
                    else if (buffer.Length == prevBuffer.Length && Base16.Encode(buffer) == Base16.Encode(prevBuffer))
                    {
                        LogHelper.Debug("Received duplicate data");
                        //currentAynchResult = server.BeginReceive(receiveDataAsync, null);
                        return;
                    }

                    var item = new ReceivedDataQueueItem();
                    item.IP = remote.Address.ToString();
                    item.Data = buffer;
                    item.Port = remote.Port;

                    this.receivedDataQueue.Enqueue(item);
                }
                else
                {
                    LogHelper.Warn("server.EndReceive warning: ar is null or p2p is stopped");
                }

                //currentAynchResult = server.BeginReceive(receiveDataAsync, null);
                return;
            }
            catch (Exception ex)
            {
                if (!(ex is SocketException))
                {
                    LogHelper.Warn(ex.ToString());
                }
                else
                {
                    LogHelper.Error(ex.Message, ex);
                }
                raiseOtherException(null);
            }
            finally
            {
                if (this.IsRunning && this.server != null)
                {
                    try
                    {
                        currentAynchResult = server.BeginReceive(receiveDataAsync, null);
                    }
                    catch
                    {
                        this.Stop();
                        this.Start(Guid.Parse(this.Identity), this.localIPText,this.localPort, this.isTrackerNode);
                    }
                }
                else
                {
                    LogHelper.Info("P2P Server is stoped or disabled.");
                }
            }
        }
        private void raiseDataReceived(P2PState state)
        {
            if (DataReceived != null)
            {
                DataReceived(state);
            }
        }
        private void raiseOtherException(P2PState state, string descrip)
        {
            if (OtherException != null)
            {
                OtherException(state);
            }
        }
        private void raiseOtherException(P2PState state)
        {
            raiseOtherException(state, "");
        }
        private void raiseNodeConnectionStateChanged(P2PNode node)
        {
            if (this.NodeConnectionStateChanged != null)
            {
                NodeConnectionStateChanged(node);
            }
        }
        private void pingMsgHandle(P2PState state)
        {
            var peer = this.Peers.Where(p => p.IP == state.IP && p.Port == state.Port).FirstOrDefault();

            if (peer == null)
            {
                if (this.isTrackerNode || this.Peers.Count(p => p.IsConnected && p.IsInbound) < this.InboundLimit)
                {
                    var newPeer = new P2PNode();
                    newPeer.IP = state.IP;
                    newPeer.Port = state.Port;
                    newPeer.Identity = state.Command.Identity;
                    newPeer.IsConnected = false;
                    newPeer.ConnectedTime = Time.EpochTime;
                    newPeer.IsTrackerServer = false;
                    newPeer.IsInbound = true;

                    this.Peers.Add(newPeer);

                    if (this.receivedMessageBuffer.ContainsKey(state.IP + ":" + state.Port))
                    {
                        this.receivedMessageBuffer[state.IP + ":" + state.Port] = new List<byte>();
                    }
                    else
                    {
                        this.receivedMessageBuffer.Add(state.IP + ":" + state.Port, new List<byte>());
                    }

                    var pongCommand = P2PCommand.CreateCommand(this.Identity, CommandNames.P2P.Pong, null);
                    this.Send(pongCommand, newPeer.IP, newPeer.Port);

                    var verPayload = new VersionMsg();
                    verPayload.Version = Versions.EngineVersion;
                    verPayload.Timestamp = Time.EpochTime;

                    var versionCommand = P2PCommand.CreateCommand(this.Identity, CommandNames.P2P.Version, verPayload);
                    this.Send(versionCommand, state.IP, state.Port);
                }
                else
                {
                    var payload = new RejectMsg();
                    payload.ReasonCode = ErrorCode.Engine.P2P.Connection.THE_NUMBER_OF_CONNECTIONS_IS_FULL;

                    var rejectCommand = P2PCommand.CreateCommand(this.Identity, CommandNames.Other.Reject, payload);
                    this.Send(rejectCommand, state.IP, state.Port);
                }
            }
            else
            {
                var pongCommand = P2PCommand.CreateCommand(this.Identity, CommandNames.P2P.Pong, null);
                this.Send(pongCommand, state.IP, state.Port);

                var verPayload = new VersionMsg();
                verPayload.Version = Versions.EngineVersion;
                verPayload.Timestamp = Time.EpochTime;

                var versionCommand = P2PCommand.CreateCommand(this.Identity, CommandNames.P2P.Version, verPayload);
                this.Send(versionCommand, state.IP, state.Port);

                if (!this.receivedMessageBuffer.ContainsKey(state.IP + ":" + state.Port))
                {
                    this.receivedMessageBuffer.Add(state.IP + ":" + state.Port, new List<byte>());
                }

                //var payload = new RejectMsg();
                //payload.ReasonCode = ErrorCode.Engine.P2P.Connection.THE_PEER_IS_EXISTED;

                //var rejectCommand = P2PCommand.CreateCommand(CommandNames.Other.Reject, payload);
                //this.Send(rejectCommand, state.IP, state.Port);
            }
        }
        private void pongMsgHandle(P2PState state)
        {
            var peer = this.Peers.Where(p => p.IP == state.IP && p.Port == state.Port).FirstOrDefault();

            if (peer != null)
            {
                var verPayload = new VersionMsg();
                verPayload.Version = Versions.EngineVersion;
                verPayload.Timestamp = Time.EpochTime;

                var versionCommand = P2PCommand.CreateCommand(this.Identity, CommandNames.P2P.Version, verPayload);
                this.Send(versionCommand, state.IP, state.Port);
                //peer.IsConnected = true;
                //peer.ConnectedTime = Time.EpochTime;
                //peer.LatestHeartbeat = Time.EpochTime;
            }
        }
        private void versionMsgHandle(P2PState state)
        {
            var peer = this.Peers.Where(p => p.IP == state.IP && p.Port == state.Port).FirstOrDefault();

            if (peer != null)
            {
                var versionMsg = new VersionMsg();
                int index = 0;
                versionMsg.Deserialize(state.Command.Payload, ref index);
                bool checkResult;

                if (versionMsg.Version < Versions.MinimumSupportVersion)
                {
                    checkResult = false;
                    var data = new RejectMsg();
                    data.ReasonCode = ErrorCode.Engine.P2P.Connection.P2P_VERSION_NOT_BE_SUPPORT_BY_REMOTE_PEER;

                    var rejectCommand = P2PCommand.CreateCommand(this.Identity, CommandNames.Other.Reject, data);
                    this.Send(rejectCommand, state.IP, state.Port);

                    this.RemovePeer(state.IP, state.Port);
                }
                else if (Math.Abs(Time.EpochTime - versionMsg.Timestamp) > 2 * 60 * 60 * 1000)
                {
                    checkResult = false;
                    var data = new RejectMsg();
                    data.ReasonCode = ErrorCode.Engine.P2P.Connection.TIME_NOT_MATCH_WITH_RMOTE_PEER;

                    var rejectCommand = P2PCommand.CreateCommand(this.Identity, CommandNames.Other.Reject, data);
                    this.Send(rejectCommand, state.IP, state.Port);
                }
                else
                {
                    peer.Version = versionMsg.Version;
                    checkResult = true;
                }

                if (checkResult)
                {
                    var verAckCommand = P2PCommand.CreateCommand(this.Identity, CommandNames.P2P.VerAck, null);
                    this.Send(verAckCommand, state.IP, state.Port);
                }
            }
        }
        private void verAckMsgHandle(P2PState state)
        {
            var peer = this.Peers.Where(p => p.IP == state.IP && p.Port == state.Port).FirstOrDefault();

            if (peer != null)
            {
                peer.IsConnected = true;
                peer.ConnectedTime = Time.EpochTime;
                peer.LastHeartbeat = Time.EpochTime;

                if (peer.IsTrackerServer)
                {
                    var msg = new HeightMsg();
                    msg.Height = this.LastBlockHeight;
                    msg.BlockTime = this.LastBlockTime;
                    var hbCommand = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.P2P.Heartbeat, msg);
                    this.Send(hbCommand, peer.IP, peer.Port);

                    var payload = new GetAddrMsg();
                    payload.Count = this.OutboundLimit;

                    var getAddrCommand = P2PCommand.CreateCommand(this.Identity, CommandNames.P2P.GetAddr, payload);
                    this.Send(getAddrCommand, peer.IP, peer.Port);
                }
                else
                {
                    this.raiseNodeConnectionStateChanged(peer);
                }
            }
        }
        private void getAddrMsgHandle(P2PState state)
        {
            var peer = this.Peers.Where(p => p.IP == state.IP && p.Port == state.Port).FirstOrDefault();

            if (peer != null && peer.IsConnected)
            {
                LogHelper.Debug($"peer.Height:{peer.LatestHeight}, peer.Version:{peer.Version}");
                var data = new GetAddrMsg();
                int index = 0;
                data.Deserialize(state.Command.Payload, ref index);

                if (data.Count <= 0/* || data.Count > maxConnections*/)
                {
                    data.Count = maxConnections;
                }

                var higherCount = data.Count;

                //var list = this.Peers.Where(p => (p.IP != state.IP || p.Port != state.Port) && p.LatestHeight > peer.LatestHeight).OrderByDescending(p => p.LastHeartbeat).Take(higherCount).ToList();
                //var shorterCount = data.Count - list.Count();
                //var list2 = this.Peers.Where(p => (p.IP != state.IP || p.Port != state.Port) && p.LatestHeight <= peer.LatestHeight).OrderByDescending(p => p.LastHeartbeat).Take(shorterCount).ToList();
                //list.AddRange(list2);
                var list = this.Peers.Where(p => (p.IP != state.IP || p.Port != state.Port) && p.LatestHeight > peer.LatestHeight && p.Version >= peer.Version)/*.OrderByDescending(p => p.Version)*/.OrderByDescending(p => p.LastHeartbeat).Take(higherCount);
                var payload = new AddrMsg();

                foreach (var item in list)
                {
                    payload.AddressList.Add(new AddrMsg.AddressInfo()
                    {
                        Ip = item.IP,
                        Port = item.Port,
                        Identity = item.Identity
                    });

                    LogHelper.Debug($"{item.IP}:{item.Port}, version={item.Version}");
                }

                var addrCommand = P2PCommand.CreateCommand(this.Identity, CommandNames.P2P.Addr, payload);
                this.Send(addrCommand, state.IP, state.Port);
            }
        }
        private void addrMsgHandle(P2PState state)
        {
            var peer = this.Peers.Where(p => p.IP == state.IP && p.Port == state.Port).FirstOrDefault();

            if (peer != null && peer.IsConnected)
            {
                var payload = new AddrMsg();
                int index = 0;
                payload.Deserialize(state.Command.Payload, ref index);

                foreach (var item in payload.AddressList)
                {
                    var peerInfo = peer.IP + ":" + peer.Port;

                    if (item.Identity != this.Identity && !this.BlackList.Contains(peerInfo))
                    {
                        if (this.Peers.Where(p => !p.IsTrackerServer && p.IP == item.Ip && p.Port == item.Port && p.IsConnected).Count() == 0)
                        {
                            this.ConnectToNewPeer(item.Ip, item.Port);
                        }
                    }
                }
            }

        }
        private void heartbeatMsgHandle(P2PState state)
        {
            var peer = this.Peers.Where(p => p.IP == state.IP && p.Port == state.Port).FirstOrDefault();

            if (peer != null && peer.IsConnected)
            {
                var payload = new HeightMsg();
                int index = 0;

                try
                {
                    payload.Deserialize(state.Command.Payload, ref index);
                    peer.LatestHeight = payload.Height;
                    peer.LatestBlockTime = payload.BlockTime;
                }
                catch
                {

                }

                peer.LastHeartbeat = Time.EpochTime;
            }
        }
        private void rejectMsgHandle(P2PState state)
        {
            var peer = this.Peers.Where(p => p.IP == state.IP && p.Port == state.Port).FirstOrDefault();

            if (peer != null && peer.IsConnected)
            {
                if (!peer.IsConnected)
                {
                    this.RemovePeer(peer.IP, peer.Port);
                    var peerInfo = peer.IP + ":" + peer.Port;

                    if (!this.BlackList.Contains(peerInfo))
                    {
                        this.BlackList.Add(peer.IP + ":" + peer.Port);
                    }
                }
                else
                {
                    raiseDataReceived(state);
                }
            }
        }
        private void pktLostMsgHandle(P2PState state)
        {
            var pktLostMsg = new PktLostMsg();
            int index = 0;
            pktLostMsg.Deserialize(state.Command.Payload, ref index);

            var item = this.sendCommandCache.FirstOrDefault(p => p.Address == state.IP && p.Port == state.Port && p.Id == pktLostMsg.Id);
            if (item != null)
            {
                var data = item.Command.GetBytes();
                var packet = new P2PPacket();
                packet.Prefix = Base16.Encode(P2PPacket.DefaultPacketPrefixBytes);
                packet.Id = Base16.Encode(item.Command.Checksum);
                packet.Count = data.Length % this.bufferSize > 0 ? data.Length / this.bufferSize + 1 : data.Length / this.bufferSize;
                packet.Index = pktLostMsg.Index;

                if (packet.Index >= packet.Count)
                {
                    return;
                }

                if (packet.Index < packet.Count - 1)
                {
                    var buffer = new byte[this.bufferSize];
                    Array.Copy(data, this.bufferSize * packet.Index, buffer, 0, buffer.Length);
                    packet.Data = buffer;
                }
                else
                {
                    var buffer = new byte[data.Length - this.bufferSize * packet.Index];
                    Array.Copy(data, this.bufferSize * packet.Index, buffer, 0, buffer.Length);
                    packet.Data = buffer;
                }

                this.sendCommandQueue.Enqueue(new P2PSendMessage
                {
                    Address = state.IP,
                    Port = state.Port,
                    Packet = packet,
                    Timestamp = Time.EpochTime
                });

                item.Timestamp = Time.EpochTime;
            }
        }
        private void pktFinishedMsgHandle(P2PState state)
        {
            var pktFinishedMsg = new PktFinishedMsg();
            int index = 0;
            pktFinishedMsg.Deserialize(state.Command.Payload, ref index);
            var item = this.sendCommandCache.FirstOrDefault(p => p.Address == state.IP && p.Port == state.Port && p.Id == pktFinishedMsg.Id);

            if (item != null)
            {
                this.sendCommandCache.Remove(item);
                LogHelper.Debug($"Finished Remove sendItems {item.Id} from {item.Address}:{item.Port}");
            }
        }
    }

    public class ReceivedDataQueueItem
    {
        public string IP { get; set; }
        public int Port { get; set; }
        public byte[] Data { get; set; }
    }

    public class ReceivedDataPacketItem
    {
        public string IP { get; set; }
        public int Port { get; set; }
        public string Id { get; set; }
        public int Count { get; set; }
        public long Timestamp { get; set; }
        public string CommandName { get; set; }
        //减少数据交互量，只重发一次
        public bool IsRequestResend { get; set; }
        public SortedList<int, P2PPacket> Packets = new SortedList<int, P2PPacket>();
    }
}
