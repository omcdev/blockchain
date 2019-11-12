


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class UtxoMsg
    {
        public string AccountId { get; set; }
        public string BlockHash { get; set; }
        public string TransactionHash { get; set; }
        public int OutputIndex { get; set; }
        public long Amount { get; set; }
        public bool IsConfirmed { get; set; }
        public bool IsWatchedOnly { get; set; }
    }
}
