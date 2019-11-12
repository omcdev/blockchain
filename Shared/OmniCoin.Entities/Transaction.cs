


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities
{
    public class Transaction
    {
        public long Id { get; set; }

        public string Hash { get; set; }

        public string BlockHash { get; set; }

        public int Version { get; set; }

        public long Timestamp { get; set; }

        public long LockTime { get; set; }

        public long TotalInput { get; set; }

        public long TotalOutput { get; set; }

        public int Size { get; set; }

        public long Fee { get; set; }

        public long ExpiredTime { get; set; }

        public bool IsDiscarded { get; set; }

        public List<Input> Inputs { get; set; }

        public List<Output> Outputs { get; set; }
    }
}
