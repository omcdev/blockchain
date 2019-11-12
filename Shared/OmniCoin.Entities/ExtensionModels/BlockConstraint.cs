


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities.ExtensionModels
{
    public class BlockConstraint
    {
        public string TransactionHash;
        public long Height;
        public long LockTime;
        public bool IsCoinBase;
    }
}
