


using OmniCoin.Consensus;
using OmniCoin.Framework;
using OmniCoin.Pool.Apis;
using OmniCoin.Pool.Sockets;
using OmniCoin.PoolMessages;
using OmniCoin.ShareModels.Msgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCoin.Pool.Commands
{
    internal static class ScoopDataCommand
    {
        internal static void Received(TcpReceiveState e, PoolCommand cmd)
        {
            var miner = PoolCache.WorkingMiners.FirstOrDefault(m => m.ClientAddress == e.Address && m.IsConnected);

            if (miner == null)
            {
                LogHelper.Info("Received invalid scoop data from " + e.Address);
                LogHelper.Info("Miner logout");
                PoolJob.TcpServer.CloseSocket(e);
                return;
            }

            if (PoolCache.CurrentTask == null || PoolCache.CurrentTask.State != MiningState.Mining)
                return;

            var msg = new ScoopDataMsg();
            int index = 0;
            msg.Deserialize(cmd.Payload, ref index);


            PoolCache.CurrentTask.LastReceiveTime = Time.EpochTime;

            var minerinfo = PoolCache.CurrentTask.MinerEfforts.FirstOrDefault(x => x.Account == miner.WalletAddress);

            if (minerinfo == null)
            {
                PoolCache.CurrentTask.MinerEfforts.Add(new Models.MinerEffort { Account = miner.WalletAddress, Effort = 1 });
            }
            else
            {
                if (minerinfo.Effort == Setting.MaxNonceCount)
                {
                    RejectCommand.Send(e);
                    return;
                }
                minerinfo.Effort++;
            }

            if (msg.BlockHeight != PoolCache.CurrentTask.CurrentBlockHeight)
            {
                LogHelper.Info("Received invalid scoop data from " + e.Address + ", nonce is " + msg.Nonce + ", height is " + msg.BlockHeight);
                LogHelper.Info("Block Height invalid , Stop and Send StartMsg");

                var stopMsg = new StopMsg
                {
                    BlockHeight = msg.BlockHeight,
                    Result = false,
                    StartTime = Time.EpochTime,
                    StopTime = Time.EpochTime
                };
                StopCommand.Send(e, stopMsg);
                Task.Delay(1000).Wait();
                var startMsg = PoolCache.CurrentTask.CurrentStartMsg;
                if(startMsg != null)
                { 
                    StartCommand.Send(e, startMsg);
                }
                return;
            }

            LogHelper.Info("Received scoop data from " + miner.ClientAddress + ", nonce is " + msg.Nonce + ", scoop number is " + msg.ScoopNumber + ", block height is " + msg.BlockHeight);

            if (msg.ScoopNumber != PoolCache.CurrentTask.CurrentScoopNumber)
            {
                LogHelper.Info("Received invalid scoop data from " + e.Address + ", nonce is " + msg.Nonce + ", ScoopNumber is " + PoolCache.CurrentTask.CurrentScoopNumber + "/" + msg.ScoopNumber);
                LogHelper.Info("Scoop Number invalid");
                return;
            }

            var verResult = POC.Verify(PoolCache.CurrentTask.BaseTarget, msg.Target);

            LogHelper.Debug("Bits:" + POC.ConvertBitsToBigInt(PoolCache.CurrentTask.BaseTarget).ToString("X").PadLeft(64, '0'));
            LogHelper.Debug("Hash:" + Base16.Encode(msg.Target));
            LogHelper.Debug("Verify Result is " + verResult);

            if (!verResult)
                return;

            ForgeMsg forgeMsg = new ForgeMsg();
            forgeMsg.Account = msg.WalletAddress;
            forgeMsg.Nonce = msg.Nonce;
            forgeMsg.StartMsgId = PoolCache.CurrentTask.Id;

            //MQApi.SendForgeBlock(msg.WalletAddress, msg.Nonce, PoolCache.CurrentTask.Id);
            RabbitMQApi.SendForgeBlock(msg.WalletAddress, msg.Nonce, PoolCache.CurrentTask.Id);
        }
    }
}
