using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using OmniCoin.MiningPool.Business;
using OmniCoin.Framework;
using OmniCoin.MiningPool.API.Config;
using OmniCoin.Tools;

namespace OmniCoin.MiningPool.API
{
    public class Program
    {
        public static void Main(string[] args)
        {            
            ServerSetting setting = ConfigurationTool.GetAppSettings<ServerSetting>("OmniCoin.MiningPool.API.conf.json", "ServerSetting");
            if(setting == null)
            {
                throw new System.Exception("ServerSetting read from OmniCoin.MiningPool.API.conf.json failed !!!");
            }

            //OmniCoin.RabbitMQ.RabbitMqSetting.CONNECTIONSTRING = config.RabbitMqConnectString;
            Pool.Redis.Setting.Init(setting.RedisTestnetConnections, setting.RedisMainnetConnections);
            MiningPool.Data.DataAccessComponent.MainnetConnectionString = setting.MySqlMainnetConnectString;
            MiningPool.Data.DataAccessComponent.TestnetConnectionString = setting.MySqlTestnetConnectString;

            GlobalParameters.IsTestnet = setting.IsTestNet;

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
