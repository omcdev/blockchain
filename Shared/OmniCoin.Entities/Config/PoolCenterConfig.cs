using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities
{
    public class PoolCenterConfig
    {
        public int GenerateBlockDelayTime { get; set; }

        public string NodeRpcMainnet { get; set; }

        public string NodeRpcTestnet { get; set; }

        public string RabbitMqConnectString { get; set; }

        public string MySqlTestnetConnectString { get; set; }

        public string MySqlMainnetConnectString { get; set; }

        public string RedisTestnetConnections { get; set; }

        public string RedisMainnetConnections { get; set; }
    }
}
