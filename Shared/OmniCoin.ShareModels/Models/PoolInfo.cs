


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.ShareModels.Models
{
    public class PoolInfo
    {
        public string PoolId { get; set; }
        public string PoolAddress { get; set; }
        public int Port { get; set; }
        public long PullTime { get; set; }
        public long MinerCount { get; set; }
    }
}
