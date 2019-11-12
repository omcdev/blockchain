


using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class NewMiningPoolMsg : BasePayload
    {
        public NewMiningPoolMsg()
        {
            MinerInfo = new MiningMsg();
        }

        public MiningMsg MinerInfo { get; set; }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            MinerInfo.Deserialize(bytes, ref index);
        }

        public override byte[] Serialize()
        {
            return MinerInfo.Serialize();
        }
    }
}