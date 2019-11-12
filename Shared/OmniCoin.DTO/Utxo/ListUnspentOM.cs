


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.DTO
{
    public class ListUnspentOM
    {
        public string txid { get; set; }
        public int vout { get; set; }
        public string address { get; set; }
        public string account { get; set; }
        public string scriptPubKey { get; set; }
        /// <summary>
        /// 无用字段 NULL
        /// </summary>
        public string redeemScript { get; set; }
        public long amount { get; set; }
        public long confirmations { get; set; }
        public bool spendable { get; set; }
        /// <summary>
        /// 无用字段，false
        /// </summary>
        public bool solvable { get; set; }
    }
}
