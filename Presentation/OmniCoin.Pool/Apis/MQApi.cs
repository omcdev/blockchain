//
//
//
//using OmniCoin.Framework;
//using OmniCoin.KafkaMQ;
//using OmniCoin.Messages;
//using OmniCoin.Pool.Commands;
//using OmniCoin.Pool.Models;
//using OmniCoin.Pool.Redis;
//using OmniCoin.Pool.Sockets;
//using OmniCoin.PoolMessages;
//using OmniCoin.RabbitMQ;
//using OmniCoin.ShareModels;
//using OmniCoin.ShareModels.Models;
//using OmniCoin.ShareModels.Msgs;
//using Newtonsoft.Json;
//using System;
//using System.Linq;
//using System.Threading.Tasks;

//namespace OmniCoin.Pool.Apis
//{
//    public class MQApi
//    {
//        public static void SendForgeBlock(string account, long nonce, string startMsgId)
//        {
//            ForgeMsg forgeMsg = new ForgeMsg();
//            forgeMsg.Account = account;
//            forgeMsg.Nonce  = nonce;
//            forgeMsg.StartMsgId = startMsgId;
//            var json = JsonConvert.SerializeObject(forgeMsg);
//            MqManager.Current.Send(MsgType.ForgetBlock, json);
//        }

//        public static MQApi Current;

//        public MQApi()
//        {
//            try
//            {
//                MqManager.Current.Regist(MsgType.StartMining, ReceiveStartMsg);
//                MqManager.Current.Regist(MsgType.StopMining, ReceiveStopMsg);
//                MqManager.Current.Regist(MsgType.Login, ReceiveLoginMsg);
//                MqManager.Current.Listen();
//            }
//            catch (Exception ex)
//            {
//                LogHelper.Error(ex.ToString());
//            }
//        }

//        static SafeCollection<string> ForgeJsons = new SafeCollection<string>();


//        public void ReceiveStartMsg(string json)
//        {
//            try
//            {
//                var msg = JsonConvert.DeserializeObject<StartMiningMsg>(json);

//                if (msg == null)
//                    return;

//                LogHelper.Info("Receive StartMsg");

//                PoolTask poolTask = new PoolTask();
                    
//                poolTask.CurrentBlockHeight = msg.BlockHeight;
//                poolTask.CurrentScoopNumber = msg.ScoopNumber;
//                poolTask.CurrentStartMsg = msg.GetStartMsg();
//                poolTask.GeneratingBlock = RedisManager.Current.GetDataInRedis<BlockMsg>(msg.Id.ToString());
//                poolTask.BaseTarget = msg.BaseTarget;
//                poolTask.StartTime = msg.StartTime;
//                poolTask.State = MiningState.Wait;
//                poolTask.Id = msg.Id;

//                PoolCache.poolTasks.Clear();
//                PoolCache.poolTasks.Add(poolTask);
//            }
//            catch(Exception ex)
//            {
//                LogHelper.Error(ex.ToString());
//                return;
//            }
//        }

//        public void ReceiveStopMsg(string json)
//        {
//            try
//            {
//                var msg = JsonConvert.DeserializeObject<StopMiningMsg>(json);

//                LogHelper.Info("Receive StopMsg");

//                if (msg == null || PoolCache.CurrentTask == null)
//                    return;

//                StopMsg stopMsg = new StopMsg();
//                stopMsg.Result = msg.StopReason == StopReason.MiningSucesses;
//                stopMsg.BlockHeight = msg.CurrentHeight;
//                stopMsg.StopTime = msg.StopTime;


//                var miners = PoolCache.WorkingMiners.ToArray();
//                foreach (Miner item in miners)
//                {
//                    try
//                    {
//                        TcpState tcpState = new TcpState() { Client = item.Client, Stream = item.Stream, Address = item.ClientAddress };
//                        StopCommand.Send(tcpState, stopMsg);
//                    }
//                    catch (Exception ex)
//                    {
//                        LogHelper.Error(ex.ToString());
//                    }
//                }

//                var stopTask = PoolCache.CurrentTask;
//                if (PoolCache.Efforts.ContainsKey(stopTask.CurrentBlockHeight))
//                {
//                    var items = PoolCache.Efforts[stopTask.CurrentBlockHeight];
//                    stopTask.MinerEfforts.ForEach(x =>
//                    {
//                        var item = items.FirstOrDefault(p => p.Account == x.Account);
//                        if (item == null)
//                        {
//                            items.Add(new EffortInfo { Account = x.Account, Effort = x.Effort, BlockHeight = stopTask.CurrentBlockHeight });
//                        }
//                        else
//                        {
//                            item.Effort += x.Effort;
//                        }
//                    });
//                }
//                else
//                {
//                    var efforts = stopTask.MinerEfforts.Select(x => new EffortInfo { Account = x.Account, Effort = x.Effort, BlockHeight = stopTask.CurrentBlockHeight }).ToList();
//                    PoolCache.Efforts.Add(stopTask.CurrentBlockHeight, efforts);
//                }

//                if (msg.StopReason == StopReason.MiningSucesses)
//                {
//                    TimerTasks.Current.SaveMinerEffortToRedis(msg.CurrentHeight);
//                    PoolCache.poolTasks.RemoveAll(x => x.CurrentBlockHeight <= stopMsg.BlockHeight);
//                }
//                PoolCache.CurrentTask = null;
//            }
//            catch (Exception ex)
//            {
//                LogHelper.Error(ex.ToString());
//            }
//        }

//        public void ReceiveLoginMsg(string json)
//        {
//            var msg = JsonConvert.DeserializeObject<MinerLoginMsg>(json);

//            if (msg == null)
//                return;

//            LogHelper.Info("Receive LoginMsg");

//            if (msg.ServerId == Setting.PoolId)
//                return;

//            var miner = PoolCache.WorkingMiners.FirstOrDefault(x => x.SerialNo == msg.SN || x.WalletAddress == msg.Account);
//            if (miner == null)
//                return;

//            TcpState tcpState = new TcpState() { Client = miner.Client, Stream = miner.Stream, Address = miner.ClientAddress };
//            StopCommand.Send(tcpState, new StopMsg
//            {
//                BlockHeight = PoolCache.CurrentTask.CurrentBlockHeight,
//                Result = false,
//                StartTime = PoolCache.CurrentTask.StartTime,
//                StopTime = Time.EpochTime
//            });
//            RejectCommand.Send(tcpState);
//            PoolCache.WorkingMiners.Remove(miner);
//        }

//        public void SendLoginMsg(MinerLoginMsg msg)
//        {
//            var json = JsonConvert.SerializeObject(msg);
//            MqManager.Current.Send(MsgType.Login, json);
//        }
//    }
//}
