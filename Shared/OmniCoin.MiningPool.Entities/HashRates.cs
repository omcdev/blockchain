using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.MiningPool.Entities
{
    public class HashRates
    {
        /// <summary>
        /// 
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
	    public long Time { get; set; }

        /// <summary>
        /// 哈希个数
        /// </summary>
        public long Hashes { get; set; }
    }
}
