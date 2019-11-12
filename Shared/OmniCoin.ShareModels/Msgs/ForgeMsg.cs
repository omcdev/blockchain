


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.ShareModels.Msgs
{
    [Serializable]
    public class ForgeMsg
    {
        public string StartMsgId;
        public string Account;
        public long Nonce;
    }
}
