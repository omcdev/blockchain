


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities.Explorer
{
    public class TransOM
    {
        public string Hash;
        public int Size;
        public long Timestamp;
        /// <summary>
        /// 确认高度
        /// </summary>
        public long OutputAffirm;
        public long OutputAmount;

        public List<InputOM> InputList;
        public List<OutputOM> OutputList;
    }

    public class InputOM
    {
        public string AccountId;
        public long Amount;
        public string OutputTransactionHash;
        public string UnlockScript;
    }

    public class OutputOM
    {
        public string ReceiverId;
        public bool Spent;
        public long Amount;
        public string LockScript;
    }
}
