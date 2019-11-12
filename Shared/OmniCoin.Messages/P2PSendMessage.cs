


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class P2PSendMessage
    {
        public string Id { get; set; }
        public long Timestamp { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public P2PCommand Command { get; set; }
        public P2PPacket Packet { get; set; }
    }
}
