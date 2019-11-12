


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.DTO
{
    public class GetPeerInfoOM
    {
        public int id { get; set; }
        public bool isTracker { get; set; }
        public string addr { get; set; }
        //public string addrLocal { get; set; }
        public long lastSend { get; set; }
        public long lastRecv { get; set; }
        public long lastHeartBeat { get; set; }
        public long bytesSent { get; set; }
        public long bytesRecv { get; set; }
        public long connTime { get; set; }
        //public long timeOffset { get; set; }
        public int version { get; set; }
        //public int subver { get; set; }
        public bool inbound { get; set; }
        public long latestHeight { get; set; }
        //public long startingHeight { get; set; }
        //public int banScore { get; set; }
        //public int synced_headers { get; set; }
        //public int synced_blocks { get; set; }
        //public InfightOM[] inflight { get; set; }
        //public bool whiteListed { get; set; }
    }

    public class InfightOM
    {
        public int blocks { get; set; }
    }
}
