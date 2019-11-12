// Copyright (c) 2018 FiiiLab Technology Ltd
// Distributed under the MIT software license, see the accompanying
// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using FiiiChain.Business;
using FiiiChain.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace FiiiChain.Node
{
    public class TransactionJob : BaseJob
    {
        bool isRunning;
        Timer timer;
        string newAccountId;

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


        public TransactionJob()
        {
            this.timer = new Timer(10000);
            this.timer.AutoReset = true;
            this.timer.Elapsed += Timer_Elapsed;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                this.transactionTest();
            }
            catch(Exception)
            {

            }
        }

        public override void Start()
        {
            isRunning = true;
            this.timer.Start();
        }

        public override void Stop()
        {
            isRunning = false;
            this.timer.Stop();
        }

        private void transactionTest()
        {
            var utxoComponent = new UtxoComponent();
            var transactionComponent = new TransactionComponent();
            var accountComponent = new AccountComponent();

            if(string.IsNullOrWhiteSpace(this.newAccountId))
            {
                var newAccount = accountComponent.GenerateNewAccount();
                this.newAccountId = newAccount.Id;
            }

            var defaultAccount = accountComponent.GetDefaultAccount();
            if(defaultAccount != null)
            {
                var balance = utxoComponent.GetConfirmedBlanace(defaultAccount.Id);
                var amount = 50000000000L;
                var fee = 1000000L;

                if (balance > (amount + fee))
                {
                    var dict = new Dictionary<string, long>();
                    dict.Add(newAccountId, amount);

                    var txMsg = transactionComponent.CreateNewTransactionMsg(defaultAccount.Id, dict, fee);
                    transactionComponent.AddTransactionToPool(txMsg);

                    LogHelper.Debug("====== A New transaction has been created. " + txMsg.Hash);

                    if(BlockchainJob.Current.P2PJob != null)
                    {
                        BlockchainJob.Current.P2PJob.BroadcastNewTransactionMessage(txMsg.Hash);
                    }
                }
            }
        }
    }
}
