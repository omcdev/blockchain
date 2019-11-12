//
//
//
//using OmniCoin.Framework;
//using OmniCoin.KafkaMQ;
//using OmniCoin.Messages;
//using OmniCoin.Pool.Redis;
//using OmniCoin.PoolMessages;
//using OmniCoin.ShareModels;
//using OmniCoin.ShareModels.Helper;
//using OmniCoin.ShareModels.Msgs;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Timers;

//namespace OmniCoin.PoolCenter.Apis
//{
//    public class MQApi
//    {
//        public static void SendStartMsg(StartMiningMsg startMsg)
//        {
//            string json = JsonConvert.SerializeObject(startMsg);
//            MqManager.Current.Send(MsgType.StartMining, json);
//        }

//        public static void SendStopMsg(StopMiningMsg stopMsg)
//        {
//            string json = JsonConvert.SerializeObject(stopMsg);
//            MqManager.Current.Send(MsgType.StopMining, json);
//        }

//        public static void Init()
//        {
//            MqManager.Current.Regist(MsgType.ForgetBlock, AddForgeMsg);
//            MqManager.Current.Regist(MsgType.HeartPool, GetHeartPoolMsg);
//            MqManager.Current.Listen();


//            Timer timer = new Timer(10);
//            timer.Elapsed += Timer_Elapsed;
//            timer.Start();
//        }

//        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
//        {
//            var timer = sender as Timer;
//            timer.Stop();
//            try
//            {
//                if (ForgeMsgs == null || !ForgeMsgs.Any())
//                    return;
//                var msg = ForgeMsgs.FirstOrDefault();
//                ForgeMsgs.Remove(msg);
//                ForgeBlock(msg);
//            }
//            catch (Exception ex)
//            {
//                LogHelper.Error(ex.ToString());
//            }
//            finally
//            {
//                timer.Start();
//            }
//        }

//        static SafeCollection<ForgeMsg> ForgeMsgs = new SafeCollection<ForgeMsg>();

//        public static void AddForgeMsg(string json)
//        {
//            try
//            {

//                var msg = JsonConvert.DeserializeObject<ForgeMsg>(json);

//                if (msg == null)
//                    return;
//                ForgeMsgs.Add(msg);
//            }
//            catch(Exception ex)
//            {
//                LogHelper.Error(ex.ToString());
//            }
//        }

//        public static void ForgeBlock(ForgeMsg msg)
//        {
//            try
//            {
//                LogHelper.Info("Forge Block 3");
                
//                LogHelper.Info($"{msg == null},{PoolCenterJob.Current == null}");
//                var startMsg = PoolCenterJob.Current.CurrentStartMiningMsg;
                
//                if (msg != null && PoolCenterJob.Current != null && startMsg != null)
//                    LogHelper.Info($"{startMsg.Id},{msg.StartMsgId}");
//                if (msg == null || PoolCenterJob.Current == null|| startMsg == null || startMsg.Id != msg.StartMsgId)
//                    return;
//                LogHelper.Info("Forge Block 4");
                
//                var blockMsg = RedisManager.Current.GetDataInRedis<BlockMsg>(msg.StartMsgId);
//                if (blockMsg == null || blockMsg.Header.Height <startMsg.BlockHeight)
//                    return;

//                LogHelper.Info("Forge Block 5");
//                if (!(blockMsg.Header.Height > NodeApi.Current.GetBlockHeight()))
//                    return;

//                LogHelper.Info("Forge Block 6");
//                LogHelper.Info("Received Msg [ForgeBlock]");

//                if (PoolCenterJob.Current.ForgeBlock(msg.StartMsgId, msg.Account, msg.Nonce))
//                {
//                    CenterCache.GenarateBlockCount++;
//                    PoolCenterJob.Current.StopMining(true, msg.StartMsgId);
                    
//                    Task.Delay(Setting.MaxMiningBlockCount).Wait();
//                    PoolCenterJob.Current.Start();

//                    Task.Run(() =>
//                    {
//                        if (PoolCenterJob.Current.RemoveStartMsgIds.ContainsKey(msg.StartMsgId))
//                        {
//                            PoolCenterJob.Current.RemoveStartMsgIds[msg.StartMsgId] = Time.EpochTime;
//                        }
//                        Task.Delay(Setting.SAVE_REWARDS_BEHIND_GENERATETIME_BLOCK).Wait();
//                        var height = Convert.ToInt32(blockMsg.Header.Height);
//                        PoolApi.SaveRewards(msg.StartMsgId, msg.Nonce, height);
//                    });
//                }
//            }
//            catch (Exception ex)
//            {
//                LogHelper.Error(ex.ToString());
//            }
//        }

//        public static void GetHeartPoolMsg(string json)
//        {
//            if (PoolCenterJob.Current == null)
//                return;

//            var msg = JsonConvert.DeserializeObject<PoolHeartMsg>(json);

//            if (msg == null)
//                return;

//            if (CenterCache.Pools.ContainsKey(msg.PoolId))
//            {
//                CenterCache.Pools[msg.PoolId] = Time.EpochTime;
//            }
//            else
//            {
//                CenterCache.Pools.Add(msg.PoolId, Time.EpochTime);
//            }
//        }
//    }
//}