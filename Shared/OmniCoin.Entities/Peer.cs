


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities
{
    public class Peer
    {
        public long Id { get; set; }

        public string IP { get; set; }

        public int Port { get; set; }

        public long PingTime { get; set; }

        public long Timestamp { get; set; }
    }
}
