


using OmniCoin.Consensus;
using OmniCoin.Framework;
using OmniCoin.Pool.Apis;
using OmniCoin.Pool.Models;
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
    internal static class LoginCommand
    {
        /// <summary>
        /// 接收消息
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cmd"></param>
        internal static void Receive(TcpReceiveState e, PoolCommand cmd)
        {
           //TaskWork.Current.Add(new Task(() =>
           //{
               var loginMsg = new LoginMsg();
               int index = 0;
               loginMsg.Deserialize(cmd.Payload, ref index);
               //验证矿工身份
               if (!MinerApi.ValidateMiner(loginMsg.WalletAddress, loginMsg.SerialNo))
               {
                   RejectCommand.Send(e);
                   return;
               }

                //TODO: address and SerialNo and account only for one Minner 第一个与条件匹配的矿工
               var miner = PoolCache.WorkingMiners.FirstOrDefault(m => m.WalletAddress == loginMsg.WalletAddress || m.ClientAddress == e.Address || m.SerialNo == loginMsg.SerialNo);
               //矿工不为空，发送stop命令
               if (miner != null)
               {
                   StopMsg stopMsg = new StopMsg();
                   stopMsg.Result = false;
                   if (PoolCache.CurrentTask == null)
                       return;
                   stopMsg.BlockHeight = PoolCache.CurrentTask.CurrentBlockHeight;
                   stopMsg.StartTime = PoolCache.CurrentTask.StartTime;
                   stopMsg.StopTime = Time.EpochTime;

                   TcpSendState tcpSendState = new TcpSendState() { Client = miner.Client, Stream = miner.Stream, Address = miner.ClientAddress };
                   StopCommand.Send(tcpSendState, stopMsg);

                   PoolCache.WorkingMiners.Remove(miner);
               }

               miner = new Miner();
               miner.SerialNo = loginMsg.SerialNo;
               miner.WalletAddress = loginMsg.WalletAddress;
               miner.ClientAddress = e.Address;
               miner.Client = e.Client;
               miner.Stream = e.Stream;

               Random random = new Random();
               miner.CheckScoopNumber = random.Next(0, POC.MAX_SCOOP_NUMBER + 1);
               PoolCache.WorkingMiners.Add(miner);

               miner.IsConnected = true;
               miner.ConnectedTime = Time.EpochTime;
               miner.LatestHeartbeatTime = Time.EpochTime;
               SendLoginResult(e, true);
               LogHelper.Info(miner.ClientAddress + " login success");

               MinerLoginMsg loginMinerMsg = new MinerLoginMsg();
               loginMinerMsg.Account = loginMsg.WalletAddress;
               loginMinerMsg.SN = loginMsg.SerialNo;
               loginMinerMsg.ServerId = Setting.PoolId;

               //MQApi.Current.SendLoginMsg(loginMinerMsg);
               RabbitMQApi.Current.SendLoginMsg(loginMinerMsg);
               if (PoolCache.CurrentTask != null)
               {
                   StartCommand.Send(e, PoolCache.CurrentTask.CurrentStartMsg);
               }
           //}));
        }

        internal static void SendLoginResult(TcpState e, bool result)
        {
            if (PoolJob.TcpServer != null)
            {
                var msg = new LoginResultMsg();
                msg.Result = result;
                var cmd = PoolCommand.CreateCommand(CommandNames.LoginResult, msg);
                PoolJob.TcpServer.SendCommand(e, cmd);
            }
        }
    }
}
