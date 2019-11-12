namespace OmniCoin.Entities.Explorer
{
    public class BlockDetailDataEx
    {
        public long Id { get; set; }
        /// <summary>
        /// Number Of Transactions[统计]
        /// </summary>
        public int TradeCount { get; set; }
        /// <summary>
        /// Output Total[统计]
        /// </summary>
        public long TotalOutput { get; set; }
        /// <summary>
        /// Block Reward
        /// </summary>
        public long BlockReward { get; set; }
        /// <summary>
        /// Transaction Fees[统计]
        /// </summary>
        public long TransactionFees { get; set; }
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
        public long TotalAmount { get; set; }
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
    }
}
