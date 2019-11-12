


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities.Explorer
{
    public class TransactionDetail : TransOM
    {
        public string BlockHash;
        public long BlockHeight;
        public long LockTime;
        public long TotalInput;
        public long TotalOutput;
        public long InputAmount;
        public long Fee;
    }
}