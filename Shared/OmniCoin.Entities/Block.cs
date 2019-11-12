


using System;
using System.Collections.Generic;

namespace OmniCoin.Entities
{
    /// <summary>
    /// 区块表，里面是区块的信息
    /// </summary>
    public class Block
    {
        public long Id { get; set; }
        public string Hash { get; set; }

        public int Version { get; set; }

        public long Height { get; set; }

        public string PreviousBlockHash { get; set; }

        public long Bits { get; set; }

        public long Nonce { get; set; }

        public long Timestamp { get; set; }

        public string NextBlockHash { get; set; }

        public long TotalAmount { get; set; }

        public long TotalFee { get; set; }

        public string GeneratorId { get; set; }

        public string BlockSignature { get; set; }
        
        public string PayloadHash { get; set; }

        public bool IsDiscarded { get; set; }

        public bool IsVerified { get; set; }

        public List<Transaction> Transactions { get; set; }
    }
}
