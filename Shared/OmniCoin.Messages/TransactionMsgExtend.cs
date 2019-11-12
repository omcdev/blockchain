using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class TransactionMsgExtend
    {
        public TransactionMsg Msg { get; set; }

        public List<SignInfo> SignList { get; set; }
    }

    public class SignInfo
    {
        public string Txid { get; set; }

        public int Vout { get; set; }

        public string Address { get; set; }
    }
}
