using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.MiningPool.Entities
{
    public class RewardList
    {
        /// <summary>
        /// 
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 区块Hash，唯一的
        /// </summary>
        public string BlockHash { get; set; }

        /// <summary>
        /// 钱包地址
        /// </summary>
        public string MinerAddress { get; set; }

        /// <summary>
        /// Hash个数, 工作量，收到的hash个数
        /// </summary>
        public long Hashes { get; set; }

        /// <summary>
        /// 原始奖励
        /// </summary>
        public long OriginalReward { get; set; }

        /// <summary>
        /// 实际奖励
        /// </summary>
        public long ActualReward { get; set; }


        /// <summary>
        /// 是否支付 0：未支付，1已支付，2作废
        /// </summary>
        public int Paid { get; set; }

        /// <summary>
        /// 生成时间时间戳
        /// </summary>
        public long GenerateTime { get; set; }

        /// <summary>
        /// 支付时间时间戳
        /// </summary>
        public long PaidTime { get; set; }

        /// <summary>
        /// 交易Hash
        /// </summary>
        public string TransactionHash { get; set; }

        /// <summary>
        /// 提成是否发放 0：未发放，1已发放
        /// </summary>
        public int IsCommissionProcessed { get; set; }

        /// <summary>
        /// 提成发放时间
        /// </summary>
        public long? CommissionProcessedTime { get; set; }

        /// <summary>
        /// 总存币金额
        /// </summary>
        public long DepositTotalAmount { get; set; }

        /// <summary>
        /// 用户存币总金额
        /// </summary>
        public long AddressDepositTotalAmount { get; set; }        

        /// <summary>
        /// 奖励类型,0 - 矿工, 1 - 超级节点，2 - 存币利息
        /// </summary>
        public int RewardType { get; set; }

        /// <summary>
        /// 用户存币的交易Hash
        /// </summary>
        public string DepositTransactionHash { get; set; }
    }
}
