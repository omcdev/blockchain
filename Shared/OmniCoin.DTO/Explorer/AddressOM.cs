using System.Collections.Generic;

namespace OmniCoin.DTO.Explorer
{
    public class AddressOM
    {
        /// <summary>
        /// 地址
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// 交易数量
        /// </summary>
        public int TransactionCount { get; set; }

        /// <summary>
        /// 收到的总额
        /// </summary>
        public decimal TotalAmount { get; set; }


        /// <summary>
        /// 最终余额
        /// </summary>
        public decimal SurplusAmount { get; set; }


        public List<TransactionDetailOM> TransactionList { get; set; }
    }
}
