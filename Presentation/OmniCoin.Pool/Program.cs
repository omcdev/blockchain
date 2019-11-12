using OmniCoin.Entities;
using OmniCoin.Framework;
using OmniCoin.Pool.Apis;
using OmniCoin.Tools;
using System;
using System.Threading.Tasks;

namespace OmniCoin.Pool
{
    class Program
    {
        static void Main(string[] args)
        {
            bool isTestNet = false;
            try
            {
                if (args[0].ToLower().Trim() == "-testnet")
                {
                    isTestNet = true;
                }
            }
            catch
            {

            }
            GlobalParameters.IsTestnet = isTestNet;
            Init();
            Start();
            Console.ReadKey();
        }

        static void Start()
        {
            Task.Run(() =>
            {
                try
                {
                    PoolJob poolJob = new PoolJob();
                    poolJob.Start();
                    poolJob.StartListen();
                    TimerTasks.Current.Init();
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex.Message, ex);
                }
            });
            Console.WriteLine("Pool Start !!!");
        }

        static void Init()
        {
            //从配置文件中读取            
            var  config = ConfigurationTool.GetAppSettings<PoolConfig>("OmniCoin.Pool.conf.json", "PoolConfig");
            if(config == null)
            {
                throw new Exception("read config from OmniCoin.Pool.conf.json failed!!!");
            }
            if (string.IsNullOrWhiteSpace(config.RabbitMqConnectString))
            {
                throw new Exception("RabbitMqConnectString from OmniCoin.Pool.conf.json can't be null or empty!!!");
            }
            
            RabbitMQ.RabbitMqSetting.CONNECTIONSTRING = config.RabbitMqConnectString;
            Redis.Setting.Init(config.RedisTestnetConnections, config.RedisMainnetConnections);
            MiningPool.Data.DataAccessComponent.MainnetConnectionString = config.MySqlMainnetConnectString;
            MiningPool.Data.DataAccessComponent.TestnetConnectionString = config.MySqlTestnetConnectString;

            Setting.Init(config.PoolMainPort, config.PoolTestPort);

            ConfigCenter.ConfigPool = config;

            RabbitMQApi.Current = new RabbitMQApi();
        }
    }
}
