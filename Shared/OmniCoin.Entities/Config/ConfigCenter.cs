using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities
{
    public class ConfigCenter
    {
        public static NodeConfig ConfigNode { get; set; }

        public static PoolConfig ConfigPool { get; set; }

        public static PoolCenterConfig ConfigPoolCenter { get; set; }
        
    }
}
