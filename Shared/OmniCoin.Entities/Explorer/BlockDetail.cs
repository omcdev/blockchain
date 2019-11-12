


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities.Explorer
{
    public class BlockDetail
    {
        public int TradeCount;
        public long TotalOutput;
        public long TransactionFees;
        public long Height;
        public long Timestamp;
        public double Difficulty;
        public long Bits;
        public long Version;
        public long Nonce;
        public long BlockReward;
        public string Hash;
        public string PreviousBlockHash;
        public string NextBlockHash;

        public List<TransOM> TranList;
    }
}
