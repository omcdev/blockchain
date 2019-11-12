


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.ShareModels.Msgs
{
    [Serializable]
    public class StopMiningMsg
    {
        public static StopMiningMsg CreateNew()
        {
            var msg = new StopMiningMsg();
            return msg;
        }
        
        public StopReason StopReason;

        public int CurrentHeight;

        public string StartMsgId;

        public long StopTime;
    }
}