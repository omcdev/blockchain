using OmniCoin.DTO.Transaction;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.MiningPool.Award
{
    public class SendReward : SendRawTransactionOutputsIM
    {
        public long OriginalReward { get; set; }
    }
}
