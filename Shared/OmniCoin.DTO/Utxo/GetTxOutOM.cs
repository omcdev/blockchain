


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.DTO
{
    public class GetTxOutOM
    {
        /// <summary>
        /// 交易所在的BlockHash
        /// </summary>
        public string bestblock { get; set; }
        public long confirmations { get; set; }
        public long value { get; set; }
        public string scriptPubKey { get; set; }
        public int version { get; set; }
        public bool coinbase { get; set; }
    }
}
