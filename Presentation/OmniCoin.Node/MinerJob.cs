// Copyright (c) 2018 FiiiLab Technology Ltd
// Distributed under the MIT software license, see the accompanying
// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using FiiiChain.Business;
using FiiiChain.Consensus;
using FiiiChain.Framework;
using FiiiChain.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FiiiChain.Node
{
    public class MinerJob : BaseJob
    {
        bool isMining = false;
        string minerName;
        BlockComponent blockComponent;
        TransactionComponent txComponent;
        Thread thread;

        public MinerJob(string minerName)
        {
            blockComponent = new BlockComponent();
            txComponent = new TransactionComponent();
            this.minerName = minerName;
        }

        public override JobStatus Status
        {
            get
            {
                if (thread == null || thread.ThreadState != ThreadState.Running)
                {
                    return JobStatus.Stopped;
                }
                else if (thread.ThreadState == ThreadState.Running && !isMining)
                {
                    return JobStatus.Stopping;
                }
                else
                {
                    return JobStatus.Running;
                }
            }
        }

        public override void Start()
        {
            this.isMining = true;
            this.thread = new Thread(new ThreadStart(this.mining));

            this.thread.IsBackground = true;
            this.thread.Start();
        }

        public override void Stop()
        {
            this.isMining = false;
        }

        private void mining()
        {
            while (this.isMining)
            {
                bool gotTheAnswer = false;
                var blockMsg = this.blockComponent.CreateNewBlock(this.minerName);

                if (this.blockMining(blockMsg))
                {
                    try
                    {
                        var latestHeight = this.blockComponent.GetLatestHeight();

                        if (blockMsg.Header.Height > latestHeight)
                        {

                            this.blockComponent.SaveBlockIntoDB(blockMsg);

                            if (BlockchainJob.Current.P2PJob != null)
                            {
                                BlockchainJob.Current.P2PJob.BroadcastNewBlockMessage(blockMsg.Header);
                            }

                            gotTheAnswer = true;
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error(ex.Message, ex);
                    }

                }

                if(!gotTheAnswer && blockMsg.Transactions.Count > 1)
                {
                    for(int i = 1; i < blockMsg.Transactions.Count; i ++)
                    {
                        var tx = blockMsg.Transactions[i];
                        try
                        {
                            this.txComponent.AddTransactionToPool(tx);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Error(ex.Message, ex);
                        }
                    }
                }

                Thread.Sleep(1000);
            }
        }

        private bool blockMining(BlockMsg block)
        {
            var work = new POW(block.Header.Height);
            var blockData = new List<byte>();

            foreach (var tx in block.Transactions)
            {
                blockData.AddRange(tx.Serialize());
            }


            Parallel.For(0L, Int64.MaxValue, new ParallelOptions { MaxDegreeOfParallelism = 4 }, (i, loopState) =>
            //for (long i = 0; i < Int64.MaxValue; i++)
            {
                //var latestHeight = this.blockComponent.GetLatestHeight();

                //if(latestHeight > -1 && latestHeight >= block.Header.Height)
                //{
                //    loopState.Stop();
                //}

                var newBuffer = new List<byte>(blockData.ToArray());
                var nonceBytes = BitConverter.GetBytes(i);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(nonceBytes);
                }

                newBuffer.AddRange(nonceBytes);
                var result = Base16.Encode(
                    HashHelper.Hash(
                        newBuffer.ToArray()
                    ));

                if (work.Verify(block.Header.Bits, result))
                {
                    block.Header.Timestamp = Time.EpochTime;
                    block.Header.Nonce = i;
                    block.Header.Hash = block.Header.GetHash();

                    //block.Transactions[0].Timestamp = block.Header.Timestamp;
                    //block.Transactions[0].Hash = block.Transactions[0].GetHash();
                    loopState.Stop();
                    //break;

                    LogHelper.Debug("A New Block " + block.Header.Height + " has been created, the correct nonce is " + i);
                }
                else
                {

                }
            }
            );

            if (block.Header.Nonce >= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
