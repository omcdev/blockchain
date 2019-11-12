using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniCoin.MiningPool.API.DTO
{
    public class MaxNonceIM
    {
        public string SN { get; set; }
        public string Address { get; set; }
        public int MaxNonce { get; set; }
        public int ScoopNumber { get; set; }
        public string ScoopData { get; set; }
    }
}
