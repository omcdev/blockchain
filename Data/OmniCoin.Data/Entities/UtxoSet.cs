


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Data.Entities
{
    public class UtxoSet
    {
        public string TransactionHash;
        public int Index;
        public string BlockHash;
        public long BlockHeight;
        public long TransactionTime;
        public long BlockTime;
        public long Locktime;
        public long DepositTime;
        public bool IsCoinbase;
        public long Amount;
        public string Account;
        public string LockScript;
        public bool IsSpent;
        public long SpentHeight;
    }
}