

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Wallet.API;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OmniCoin.Framework;
using Microsoft.Extensions.Logging;
using OmniCoin.Entities;

namespace OmniCoin.Node
{
    public class RpcJob : BaseJob
    {
        bool isRunning;        
        public override JobStatus Status
        {
            get
            {
                if (!isRunning)
                {
                    return JobStatus.Stopped;
                }
                else
                {
                    return JobStatus.Running;
                }
            }
        }
        
        public override void Start()
        {
            Task.Run(() => {
                var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
                var pathToContentRoot = Path.GetDirectoryName(pathToExe);                
                Startup.P2PStartAction = BlockchainJob.Current.P2PJob.Start;
                Startup.P2PStopAction = BlockchainJob.Current.P2PJob.Stop;
                Startup.P2PBroadcastBlockHeaderAction = BlockchainJob.Current.P2PJob.BroadcastNewBlockMessage;
                Startup.P2PBroadcastTransactionAction = BlockchainJob.Current.P2PJob.BroadcastNewTransactionMessage;
                Startup.GetLatestBlockChainInfoFunc = BlockchainJob.Current.P2PJob.GetLatestBlockChainInfo;
                Startup.EngineStopAction = BlockchainJob.Current.Stop;
                Startup.GetEngineJobStatusFunc = BlockchainJob.Current.GetJobStatus;

                //var nodePort = GlobalParameters.IsTestnet ? Properties.Resources.TestnetNodePort : Properties.Resources.MainnetNodePort;
                string url = string.Format("http://*:{0}", GlobalParameters.IsTestnet ? ConfigCenter.ConfigNode.RpcPortTestNet : ConfigCenter.ConfigNode.RpcPortMainNet);

                var host = WebHost
                  .CreateDefaultBuilder()
                    .ConfigureLogging((context, logging) =>
                    {
                        logging.ClearProviders();
                    })
                  .UseKestrel().ConfigureServices(cssc => cssc.AddMemoryCache())
                  .UseStartup<Startup>()
                  .UseUrls(url)
                  .Build();
                host.Start();
            });

            isRunning = true;
        }

        public override void Stop()
        {
            isRunning = false;
        }
    }
}
