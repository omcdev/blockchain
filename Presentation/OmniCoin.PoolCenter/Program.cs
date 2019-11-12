using OmniCoin.Entities;
using OmniCoin.Framework;
using OmniCoin.PoolCenter.Apis;
using OmniCoin.ShareModels;
using OmniCoin.Tools;
using System;

namespace OmniCoin.PoolCenter
{
    class Program
    {
        static void Main(string[] args)
        {
            var poolArgs = AnalysisArgs(args);

            if (poolArgs == null)
            {
                ShowCommandHelp();
                return;
            }

           Init(poolArgs);

           if (!Validater.PoolAccount(poolArgs.Account))
            {
                LogHelper.Error("Wallet Address is invalid");
                return;
            }
            
            Start(poolArgs);
            
            Console.ReadKey();
        }

        static void Init(PoolArgs args)
        {
            GlobalParameters.IsTestnet = args.IsTestNet;

            PoolCenterConfig config = ConfigurationTool.GetAppSettings<PoolCenterConfig>("OmniCoin.PoolCenter.conf.json", "PoolCenterSetting");

            if (config == null)
            {
                throw new Exception("read config from OmniCoin.PoolCenter.conf.json failed!!!");
            }
            if (string.IsNullOrWhiteSpace(config.RabbitMqConnectString))
            {
                throw new Exception("RabbitMqConnectString from OmniCoin.PoolCenter.conf.json can't be null or empty!!!");
            }

            RabbitMQ.RabbitMqSetting.CONNECTIONSTRING = config.RabbitMqConnectString;
            Pool.Redis.Setting.Init(config.RedisTestnetConnections, config.RedisMainnetConnections);
            MiningPool.Data.DataAccessComponent.MainnetConnectionString = config.MySqlMainnetConnectString;
            MiningPool.Data.DataAccessComponent.TestnetConnectionString = config.MySqlTestnetConnectString;

            ConfigCenter.ConfigPoolCenter = config;

            Setting.Init(config.NodeRpcMainnet, config.NodeRpcTestnet);

            

            //KafkaMQ.KafkaInfo.MqName = Setting.CENTERKAFKAGROUPNAME;
            NodeApi.Current = new NodeApi(Setting.API_URL);
            //MQApi.Init();
            RabbitMQApi.Init();
        }

        private static void Start(PoolArgs args)
        {
            try
            {
                //MQApi.SendStopMsg(new ShareModels.Msgs.StopMiningMsg { StopReason = StopReason.ReStart });
                RabbitMQApi.SendStopMsg(new ShareModels.Msgs.StopMiningMsg { StopReason = StopReason.ReStart });
                PoolCenterJob.Current = new PoolCenterJob(args);
                PoolCenterJob.Current.Start();
                PoolCenterJob.Current.StartListen();
                LogHelper.Info("OmniCoin PoolCenter Start !!!");

                TimerTasks.Current.Init();
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.ToString());
            }
        }
        
        static PoolArgs AnalysisArgs(string[] args)
        {
            PoolArgs poolArgs = null;
            if (args.Length < 2 || args.Length > 4)
                return poolArgs;
            if (args.Length == 2)
            {
                poolArgs = new PoolArgs() { Name = args[0], Account = args[1] };
            }
            else if (args.Length == 3)
            {
                if (args[0].ToLower() == "-testnet")
                {
                    poolArgs = new PoolArgs()
                    {
                        IsTestNet = args[0].ToLower() == "-testnet",
                        Name = args[1],
                        Account = args[2]
                    };
                }
                else
                {
                    poolArgs = new PoolArgs()
                    {
                        Name = args[0],
                        Account = args[1],
                        Password = args[2]
                    };
                }
            }
            else if (args.Length == 4)
            {
                poolArgs = new PoolArgs()
                {
                    IsTestNet = args[0].ToLower() == "-testnet",
                    Name = args[1],
                    Account = args[2],
                    Password = args[3]
                };
            }
            return poolArgs;
        }

        static void ShowCommandHelp()
        {
            LogHelper.Info("Usage: ");
            LogHelper.Info("\t dotnet OmniCoin.MiningPool.dll <MinerName> <WalletAddress> [Wallet Password]");
        }
    }


    public class PoolArgs
    {
        public bool IsTestNet = false;
        public string Name;
        public string Account;
        public string Password;
    }
}