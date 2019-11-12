using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities
{
    public class ListSinceBlock
    {
        public string LastBlock { get; set; }

        public SinceBlock[] Transactions { get; set; }
    }

    public class SinceBlock
    {
        public string Account { get; set; }

        public string Address { get; set; }

        public string Category { get; set; }

        public long amount { get; set; }

        public long Vout { get; set; }

        public long Fee { get; set; }

        public long Confirmations { get; set; }

        public string BlockHash { get; set; }

        public long BlockTime { get; set; }

        public string TxId { get; set; }

        public string Label { get; set; }

        public bool IsSpent { get; set; }

        public long LockTime { get; set; }

    }
}
