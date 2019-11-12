


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.DTO
{
    public class GetTransactionOM
    {
        public long amount { get; set; }
        public long fee { get; set; }
        public long confirmations { get; set; }
        public bool generated { get; set; }
        public string blockHash { get; set; }
        public int blockIndex { get; set; }
        public string txId { get; set; }
        public string[] walletConflicts { get; set; }
        public long time { get; set; }
        public long timereceived { get; set; }
        public string comment { get; set; }
        public string to { get; set; }
        public Detail[] details { get; set; }
        public string hex { get; set; }

        public class Detail
        {
            public bool involvesWatchonly { get; set; }
            public string account { get; set; }
            public string address { get; set; }
            public string category { get; set; }
            public long amount { get; set; }
            public int vout { get; set; }
            public long fee { get; set; }
            public bool abandoned { get; set; }
        }
    }
}
