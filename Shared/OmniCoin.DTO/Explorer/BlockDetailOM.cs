using System.Collections.Generic;

namespace OmniCoin.DTO.Explorer
{
    public class BlockDetailOM
    {
        public long Id { get; set; }
        /// <summary>
        /// Number Of Transactions[统计]
        /// </summary>
        public int TradeCount { get; set; }
        /// <summary>
        /// Output Total[统计]
        /// </summary>
        public decimal TotalOutput { get; set; }
        ///// <summary>
        ///// Estimated Transaction Volume[不需要]
        ///// </summary>
        //public long EstimatedAmount { get; set; }
        /// <summary>
        /// Transaction Fees
        /// </summary>
        public decimal TransactionFees { get; set; }
        /// <summary>
        /// Height
        /// </summary>
        public long Height { get; set; }
        /// <summary>
        /// Timestamp
        /// </summary>
        public long Timestamp { get; set; }
        ///// <summary>
        ///// Received Time
        ///// </summary>
        //public long ReceivedTime { get; set; }

        /// <summary>
        /// Difficulty
        /// </summary>
        public double Difficulty { get; set; }
        /// <summary>
        /// Bits
        /// </summary>
        public long Bits { get; set; }
        /// <summary>
        /// Version
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// Nonce
        /// </summary>
        public long Nonce { get; set; }
        /// <summary>
        /// TotalAmount
        /// </summary>
        public decimal TotalAmount { get; set; }
        /// <summary>
        /// Block Reward
        /// </summary>
        public decimal BlockReward { get; set; }
        /// <summary>
        /// Hash
        /// </summary>
        public string Hash { get; set; }
        /// <summary>
        /// PreviousBlockHash
        /// </summary>
        public string PreviousBlockHash { get; set; }
        /// <summary>
        /// NextBlockHash
        /// </summary>
        public string NextBlockHash { get; set; }

        public List<TransactionDetailOM> TransactionList { get; set; }
    }
}
