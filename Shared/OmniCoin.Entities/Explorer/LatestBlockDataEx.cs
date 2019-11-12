namespace OmniCoin.Entities.Explorer
{
    public class LatestBlockDataEx
    {
        public long Id { get; set; }

        public string BlockHash { get; set; }

        /// <summary>
        /// Height
        /// </summary>
        public long Height { get; set; }

        /// <summary>
        /// Transactions[统计]
        /// </summary>
        public int TradeCount { get; set; }

        /// <summary>
        /// Total Sent
        /// </summary>
        public long TotalAmount { get; set; }

        /// <summary>
        /// Size [统计]
        /// </summary>
        public long TotalSize { get; set; }
    }
}
