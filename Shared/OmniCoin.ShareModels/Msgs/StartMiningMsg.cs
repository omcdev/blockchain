using OmniCoin.Framework;
using OmniCoin.Messages;
using OmniCoin.PoolMessages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OmniCoin.ShareModels.Msgs
{
    [Serializable]
    public class StartMiningMsg
    {
        public static StartMiningMsg CreateNew()
        {
            StartMiningMsg startMiningMsg = new StartMiningMsg();
            startMiningMsg.Id = Guid.NewGuid().ToString();
            return startMiningMsg;
        }
        
        public long BlockHeight { get; set; }
        public int ScoopNumber { get; set; }
        public long StartTime { get; set; }
        public string GenHash { get; set; }
        public long BaseTarget;
        public string Id;

        public StartMsg GetStartMsg()
        {
            return new StartMsg
            {
                BlockHeight = this.BlockHeight,
                GenHash = Base16.Decode(GenHash),
                ScoopNumber = this.ScoopNumber,
                StartTime = this.StartTime
            };
        }
    }
}