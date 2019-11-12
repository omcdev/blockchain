using OmniCoin.Framework;
using OmniCoin.Messages;
using OmniCoin.Pool.Redis;
using OmniCoin.RabbitMQ;
using OmniCoin.ShareModels;
using OmniCoin.ShareModels.Msgs;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using OmniCoin.MiningPool.Business;
using OmniCoin.MiningPool.Entities;
using OmniCoin.Consensus;
using System.Collections.Generic;

namespace OmniCoin.PoolCenter.Apis
{
    public class RabbitMQApi
    {
        public static void SendStartMsg(StartMiningMsg startMsg)
        {
            string json = JsonConvert.SerializeObject(startMsg);
            RabbitMqClient.Current.ProduceMessage(RabbitMqName.StartMining, MsgType.StartMining, json);
        }

        public static void SendStopMsg(StopMiningMsg stopMsg)
        {
            string json = JsonConvert.SerializeObject(stopMsg);
            RabbitMqClient.Current.ProduceMessage(RabbitMqName.StopMining, MsgType.StopMining, json);
        }

        public static void Init()
        {
            RabbitMqClient.Current.Regist(MsgType.ForgetBlock, AddForgeMsg);
            RabbitMqClient.Current.Regist(MsgType.HeartPool, GetHeartPoolMsg);
            RabbitMqClient.Current.Listen();

            Timer timer = new Timer(10);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            Timer updateDepositStatusTimer = new Timer(1000 * 60);//1分钟更新一次
            updateDepositStatusTimer.Elapsed += UpdateDepositStatusTimer_Elapsed;
            updateDepositStatusTimer.Start();
        }

        /// <summary>
        /// 更新存币记录表的到期状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void UpdateDepositStatusTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var timerDeposit = sender as Timer;
            timerDeposit.Stop();
            try
            {
                new RewardListComponent().UpdateDepositStatus();
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.ToString());
            }
            finally
            {
                timerDeposit.Start();
            }
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var timer = sender as Timer;
            timer.Stop();
            try
            {
                if (ForgeMsgs == null || !ForgeMsgs.Any())
                {
                    return;
                }
                var msg = ForgeMsgs.FirstOrDefault();
                ForgeMsgs.Remove(msg);
                ForgeBlock(msg);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.ToString());
            }
            finally
            {
                timer.Start();
            }
        }

        static SafeCollection<ForgeMsg> ForgeMsgs = new SafeCollection<ForgeMsg>();

        public static void AddForgeMsg(string json)
        {
            try
            {

                var msg = JsonConvert.DeserializeObject<ForgeMsg>(json);

                if (msg == null)
                    return;
                ForgeMsgs.Add(msg);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.ToString());
            }
        }

        public static void ForgeBlock(ForgeMsg msg)
        {
            try
            {
                LogHelper.Info("Forge Block 3");

                LogHelper.Info($"msg == null : {msg == null} ; PoolCenterJob.Current == null : {PoolCenterJob.Current == null}");
                var startMsg = PoolCenterJob.Current.CurrentStartMiningMsg;

                if (msg != null && PoolCenterJob.Current != null && startMsg != null)
                {
                    LogHelper.Info($"{startMsg.Id},{msg.StartMsgId}");
                }
                if (msg == null || PoolCenterJob.Current == null || startMsg == null || startMsg.Id != msg.StartMsgId)
                {
                    return;
                }
                LogHelper.Info("Forge Block 4");

                var blockMsg = RedisManager.Current.GetDataInRedis<BlockMsg>(msg.StartMsgId);
                if (blockMsg == null || blockMsg.Header.Height < startMsg.BlockHeight)
                {
                    return;
                }

                LogHelper.Info("Forge Block 5");
                if (!(blockMsg.Header.Height > NodeApi.Current.GetBlockHeight()))
                {
                    return;
                }

                LogHelper.Info("Forge Block 6");
                LogHelper.Info("Received Msg [ForgeBlock]");
                BlockMsg successBlock = null;
                if (PoolCenterJob.Current.ForgeBlock(msg.StartMsgId, msg.Account, msg.Nonce,out successBlock))
                {
                    CenterCache.GenarateBlockCount++;
                    PoolCenterJob.Current.StopMining(true, msg.StartMsgId);

                    //分析存币交易
                    ProcessDepositTx(successBlock);
                    Task.Delay(Setting.MaxMiningBlockCount).Wait();
                    PoolCenterJob.Current.Start();

                    Task.Run(() =>
                    {
                        if (PoolCenterJob.Current.RemoveStartMsgIds.ContainsKey(msg.StartMsgId))
                        {
                            PoolCenterJob.Current.RemoveStartMsgIds[msg.StartMsgId] = Time.EpochTime;
                        }                        
                        Task.Delay(Setting.SAVE_REWARDS_BEHIND_GENERATETIME_BLOCK).Wait();
                        var height = Convert.ToInt32(blockMsg.Header.Height);
                        PoolApi.SaveRewards(msg.StartMsgId, msg.Nonce, height);
                    });
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.ToString());
            }
        }

        /// <summary>
        /// 存币交易分析处理
        /// </summary>
        /// <param name="successBlock"></param>
        private static void ProcessDepositTx(BlockMsg successBlock)
        {
            if(successBlock == null || successBlock.Transactions == null || !successBlock.Transactions.Any())
            {
                return;
            }
            List<DepositList> toInsert = new List<DepositList>();
            successBlock.Transactions.ForEach(x =>
            {
                if(x.DepositTime > 0 && x.Outputs != null && x.Outputs.Any())
                {
                    x.Outputs.ForEach(o =>
                   {
                       
                       var receiverId = AccountIdHelper.CreateAccountAddressByPublicKeyHash(
                        Base16.Decode(
                            Script.GetPublicKeyHashFromLockScript(o.LockScript)
                        ));
                       var dep = new DepositList()
                       {
                           Address = receiverId,
                           Amount = o.Amount,
                           ExpireTime = x.DepositTime,
                           IsExpired = x.DepositTime < Time.EpochTime ? 1:0,
                           TransactionHash = x.Hash
                       };
                       toInsert.Add(dep);
                   });
                }
            });
            if (toInsert.Any())
            {
                new RewardListComponent().InsertDeposit(toInsert);                
            }
        }

        public static void GetHeartPoolMsg(string json)
        {
            if (PoolCenterJob.Current == null)
            {
                return;
            }

            var msg = JsonConvert.DeserializeObject<PoolHeartMsg>(json);

            if (msg == null)
            {
                return;
            }

            if (CenterCache.Pools.ContainsKey(msg.PoolId))
            {
                CenterCache.Pools[msg.PoolId] = Time.EpochTime;
            }
            else
            {
                CenterCache.Pools.Add(msg.PoolId, Time.EpochTime);
            }
        }
    }
}
