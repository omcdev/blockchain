using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities
{
    public class BalanceHelper
    {
        public string TransactionHash { get; set; }
        public string BlockHash { get; set; }
        public long TotalInput { get; set; }
        public long Height { get; set; }
        public bool IsVerified { get; set; }
        public long LockTime { get; set; }
        public long Amount { get; set; }
        public bool IsDiscarded { get; set; }
    }
}
