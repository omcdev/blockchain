


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class P2PNode
    {
        public P2PNode()
        {

        }

        public P2PNode(string ip, int port)
        {
            this.IP = ip;
            this.Port = port;
        }

        public string IP { get; set; }

        public int Port { get; set; }

        public string Identity { get; set; }

        public bool IsConnected { get; set; }

        public int Version { get; set; }

        public long LastHeartbeat { get; set; }

        public long ConnectedTime { get; set; }

        public bool IsTrackerServer { get; set; }

        public long LatestHeight { get; set; }

        public long LatestBlockTime { get; set; }

        public bool IsInbound { get; set; }

        public long TotalBytesSent { get; set; }

        public long TotalBytesReceived { get; set; }

        public long LastSentTime { get; set; }

        public long LastReceivedTime { get; set; }

        public int BanScore { get; set; }

        public string LastCommand { get; set; }
    }
}
