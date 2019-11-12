using System;

namespace OmniCoin.MiningPool.Entities
{
    public class Blocks
    {
        /// <summary>
        /// 
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 区块Hash
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// 区块高度
        /// </summary>
        public long Height { get; set; }

        /// <summary>
        /// 区块生成时间戳
        /// </summary>
        public long Timstamp { get; set; }

        /// <summary>
        /// 生成区块矿工钱包地址
        /// </summary>
        public string Generator { get; set; }

        /// <summary>
        /// 生成区块随机数
        /// </summary>
        public long Nonce { get; set; }

        /// <summary>
        /// 总的奖励
        /// </summary>
        public long TotalReward { get; set; }

        /// <summary>
        /// 总的Hash，总工作量
        /// </summary>
        public long TotalHash { get; set; }

        /// <summary>
        /// 区块是否确认 0:未确认，1：已确认
        /// </summary>
        public int Confirmed { get; set; }

        /// <summary>
        /// 区块是否作废，0：正常，1：已作废
        /// </summary>
        public int IsDiscarded { get; set; }

        /// <summary>
        /// 区块奖励是否发放，0：未发放，1：已发放
        /// </summary>
        public int IsRewardSend { get; set; }
    }
}
