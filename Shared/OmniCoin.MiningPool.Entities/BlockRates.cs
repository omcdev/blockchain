using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.MiningPool.Entities
{
    public class BlockRates
    {
        public long Id { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
	    public long Time { get; set; }

        /// <summary>
        /// 区块个数
        /// </summary>
        public long Blocks { get; set; }

        /// <summary>
        /// 困难度
        /// </summary>
        public long Difficulty { get; set; }
    }
}
