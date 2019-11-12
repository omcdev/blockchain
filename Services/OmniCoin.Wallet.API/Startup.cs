

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.DTO;
using OmniCoin.Framework;
using OmniCoin.Messages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace OmniCoin.Wallet.API
{
    public class Startup
    {
        public static Action P2PStartAction;
        public static Action P2PStopAction;
        public static Action<BlockHeaderMsg> P2PBroadcastBlockHeaderAction;
        public static Action<string> P2PBroadcastTransactionAction;
        public static Action EngineStopAction;
        //lockUnspentList
        public static List<ListLockUnspentOM> lockUnspentList = new List<ListLockUnspentOM>();
        /// <summary>
        /// Get latest blockchain info
        /// </summary>
        public static Func<BlockChainInfo> GetLatestBlockChainInfoFunc;
        public static Func<Dictionary<string, string>> GetEngineJobStatusFunc;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddJsonRpc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //app.UseJsonRpc();
            app.UseManualJsonRpc(builder =>
            {
                builder.RegisterController<UtxoController>();
                builder.RegisterController<TransactionController>();
                builder.RegisterController<AccountController>();
                builder.RegisterController<AddressBookController>();
                builder.RegisterController<PaymentRequestController>();
                builder.RegisterController<WalletController>();
                builder.RegisterController<MemPoolController>();
                builder.RegisterController<NetworkController>();
                builder.RegisterController<MiningPoolController>();
                builder.RegisterController<BlockChainController>();
                builder.RegisterController<DaemonToolController>();
                if (GlobalParameters.IsExplorer)
                {
                    builder.RegisterController<ExplorerController>();
                }
            });
            //if (env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //}


            //app.Run(async (context) =>
            //{
            //    await context.Response.WriteAsync("Hello World!");
            //});
        }
    }
}
