


using OmniCoin.ShareModels.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.PoolCenter
{
    public class CenterCache
    {
        /// <summary>
        /// 记录 矿池 服务ID 和 最后心跳时间
        /// </summary>
        public static Dictionary<string, long> Pools = new Dictionary<string, long>();

        public static long GenarateBlockCount = 0;
    }
}