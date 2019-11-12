using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace FiiiChain.DataAgent
{
    public class P2PNode
    {
        public string IP { get; set; }

        public int Port { get; set; }

        public bool IsConnected { get; set; }

        public long LatestHeartbeat { get; set; }

        public long ConnectedTime { get; set; }
        
        public bool IsTrackerServer { get; set; }
    }
}
