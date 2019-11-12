using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.MiningPool.Entities
{
    public class DepositStatistics
    {             
        /// <summary>
        /// 所有地址当前未到期的总存币金额
        /// </summary>
        public long TotalAmount { get; set; }

        /// <summary>
        /// 地址的未到期的总存币金额
        /// </summary>
        public long AddressAmount { get; set; }

        /// <summary>
        /// 存币地址
        /// </summary>
        public string Address { get; set; }        
    }
}
