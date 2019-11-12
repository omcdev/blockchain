using FiiiChain.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace FiiiChain.DataAgent
{
    public class P2PSendMessage
    {
        public string address { get; set; }
        public int Port { get; set; }
        public P2PCommand Command { get; set; }
    }
}
