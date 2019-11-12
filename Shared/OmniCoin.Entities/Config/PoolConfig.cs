using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities
{
    public class PoolConfig
    {        
        int _PoolMainPort = 58807;
        public int PoolMainPort
        {
            get { return _PoolMainPort; }
            set { _PoolMainPort = value; }
        }
        int _PoolTestPort = 58808;
        public int PoolTestPort
        {
            get { return _PoolTestPort; }
            set { _PoolTestPort = value; }
        }        
        public string RabbitMqConnectString { get; set; }

        public string MySqlTestnetConnectString { get; set; }

        public string MySqlMainnetConnectString { get; set; }

        public string RedisTestnetConnections { get; set; }

        public string RedisMainnetConnections { get; set; }
    }
}
