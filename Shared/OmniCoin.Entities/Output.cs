


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities
{
    public class Output
    {
        public long Id { get; set; }

        public int Index { get; set; }

        public string TransactionHash { get; set; }

        public string ReceiverId { get; set; }

        public long Amount { get; set; }

        public int Size { get; set; }

        public string LockScript { get; set; }

        public bool Spent { get; set; }

        public bool IsDiscarded { get; set; }

        public string BlockHash { get; set; }

        public override string ToString()
        {
            return string.Format("{0}_{1}", TransactionHash, Index);
        }
    }
}
