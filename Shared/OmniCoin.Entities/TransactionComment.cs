


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities
{
    public class TransactionComment
    {
        public long Id { get; set; }
        public string TransactionHash { get; set; }
        public int OutputIndex { get; set; }
        public string Comment { get; set; }
        public long Timestamp { get; set; }
    }
}
