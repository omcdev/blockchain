using System;
using System.Collections.Generic;
using System.Text;

namespace MultiThreadingMiners
{
    public class MaxNonceIM
    {
        public string SN { get; set; }
        public string Address { get; set; }
        public int MaxNonce { get; set; } = 131072;
        public int ScoopNumber { get; set; } = 4096;
        public string ScoopData { get; set; }
    }
}
