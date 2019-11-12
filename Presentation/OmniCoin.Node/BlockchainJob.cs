

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Business;
using OmniCoin.Consensus;
using OmniCoin.Data.Dacs;
using OmniCoin.DataAgent;
using OmniCoin.Entities;
using OmniCoin.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OmniCoin.Node
{
    public class BlockchainJob : BaseJob
    {
        public static BlockchainJob Current = null;
        public BlockJob BlockService;
        public P2PJob P2PJob;
        public RpcJob RpcService;
        public bool IsRunning = true;

        public static NodeConfig config = null;

        public BlockchainJob()
        {
            RpcService = new RpcJob();
            P2PJob = new P2PJob();
            BlockService = new BlockJob();
        }
        public override JobStatus Status
        {
            get
            {
                if (P2PJob.Status == JobStatus.Running &&
                    RpcService.Status == JobStatus.Running &&
                    BlockService.Status == JobStatus.Running)
                {
                    return JobStatus.Running;
                }
                else if (P2PJob.Status == JobStatus.Stopped &&
                    RpcService.Status == JobStatus.Stopped &&
                    BlockService.Status == JobStatus.Stopped)
                {
                    return JobStatus.Stopped;
                }
                else
                {
                    return JobStatus.Stopping;
                }
            }
        }

        public override void Start()
        {
            P2PJob.Start();
            RpcService.Start();
            BlockService.Start();
            IsRunning = true;
            while (true)
            {
                if (!IsRunning)
                {
                    break;
                }
                Thread.Sleep(1000);
            }
        }

        public override void Stop()
        {
            P2PJob.Stop();
            RpcService.Stop();
            BlockService.Stop();
            IsRunning = false;
        }

        public Dictionary<string, string> GetJobStatus()
        {
            var dict = new Dictionary<string, string>();

            dict.Add("ChainService", this.Status.ToString());
            dict.Add("P2pService", P2PJob.Status.ToString());
            dict.Add("BlockService", BlockService.Status.ToString());
            dict.Add("RpcService", RpcService.Status.ToString());
            dict.Add("ChainNetwork", GlobalParameters.IsTestnet ? "Testnet" : "Mainnet");
            dict.Add("Height", new BlockComponent().GetLatestHeight().ToString());

            return dict;
        }

        public string GetAccountByLockScript(string lockScript)
        {
            var publicKeyHash = Base16.Decode(Script.GetPublicKeyHashFromLockScript(lockScript));
            var address = AccountIdHelper.CreateAccountAddressByPublicKeyHash(publicKeyHash);
            return address;
        }

        public static void Initialize()
        {
            var notify = new NotifyComponent();
            BlockchainComponent blockChainComponent = new BlockchainComponent();
            AccountComponent accountComponent = new AccountComponent();

            BlockchainJob.Current = new BlockchainJob();

            BlockDac.Default.GetAccountByLockScript = BlockchainJob.Current.GetAccountByLockScript;

            //从配置文件中读取
            ConfigurationTool tool = new ConfigurationTool();
            config = tool.GetAppSettings<NodeConfig>("NodeConfig");
            if(config == null)
            {
                GlobalParameters.IsPool = false;
                config = new NodeConfig();
            }
            else
            {
                notify.SetCallbackApp(config.WalletNotify);
                BlockchainJob.Current.P2PJob.LocalIP = config.LocalIP;
                GlobalParameters.IsPool = config.IsPool;
            }

            ConfigCenter.ConfigNode = config;

            
            

            //TODO 待验证删除
            //if (config != null)
            //{
            //    notify.SetCallbackApp(config.WalletNotify);
            //    BlockchainJob.Current.P2PJob.IPAddress = config.Ip;
            //    GlobalParameters.IsPool = config.IsPool;
            //}
            //else
            //{
            //    GlobalParameters.IsPool = false;
            //}

            if (GlobalActions.TransactionNotifyAction == null)
            {
                GlobalActions.TransactionNotifyAction = NewTransactionNotify;
            }

            blockChainComponent.Initialize();
            var accounts = AccountDac.Default.GetAccountBook();
            if (accounts.Count == 0)
            {
                var account = accountComponent.GenerateNewAccount(false);
                accountComponent.SetDefaultAccount(account.Id);
            }

            var defaultAccount = AppDac.Default.GetDefaultAccount();
            if (string.IsNullOrEmpty(defaultAccount))
            {
                var first = AccountDac.Default.GetAccountBook().FirstOrDefault();
                UserSettingComponent component = new UserSettingComponent();
                component.SetDefaultAccount(first);
            }

            TransactionPool.Instance.Load();
        }

        public static void NewTransactionNotify(string txHash)
        {
            NotifyComponent notify = new NotifyComponent();
            notify.ProcessNewTxReceived(txHash);
        }
    }
}
