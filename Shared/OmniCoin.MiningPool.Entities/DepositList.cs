using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.MiningPool.Entities
{
    public class DepositList
    {
        /// <summary>
        /// 
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 存币的交易Hash
        /// </summary>
        public string TransactionHash { get; set; }

        /// <summary>
        /// 存币金额
        /// </summary>
        public long Amount { get; set; }

        /// <summary>
        /// 存币到期时间戳
        /// </summary>
        public long ExpireTime { get; set; }

        /// <summary>
        /// 存币地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 是否到期,1为到期，0为未到期
        /// </summary>
        public int IsExpired { get; set; }
    }
}
