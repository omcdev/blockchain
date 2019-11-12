using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniCoin.MiningPool.API.Config
{
    public class ServerSetting
    {
        public long ServerRefreshTime { get; set; }

        public long MinerAmount { get; set; }

        public List<ServerInfo> ServerInfoList { get; set; }

        public bool IsTestNet { get; set; }

        public string RabbitMqConnectString { get; set; }

        public string MySqlTestnetConnectString { get; set; }

        public string MySqlMainnetConnectString { get; set; }

        public string RedisTestnetConnections { get; set; }

        public string RedisMainnetConnections { get; set; }
    }

    public class ServerInfo
    {
        public string Name { get; set; }

        public string IPAddress { get; set; }

        public int port { get; set; }

        public long MinerCount { get; set; }
    }
}
