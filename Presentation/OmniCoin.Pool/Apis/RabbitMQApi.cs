using OmniCoin.Framework;
using OmniCoin.RabbitMQ;
using OmniCoin.ShareModels;
using OmniCoin.Pool.Redis;
using OmniCoin.ShareModels.Msgs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using OmniCoin.Messages;
using OmniCoin.PoolMessages;
using OmniCoin.Pool.Models;
using OmniCoin.Pool.Sockets;
using OmniCoin.Pool.Commands;
using System.Linq;
using OmniCoin.ShareModels.Models;

namespace OmniCoin.Pool.Apis
{
    /// <summary>
    /// RabbitMQ消息队列接口
    /// </summary>
    public class RabbitMQApi
    {
        /// <summary>
        /// RabbitMQ发送ForgetMsg信息
        /// </summary>
        /// <param name="account"></param>
        /// <param name="nonce"></param>
        /// <param name="startMsgId"></param>
        public static void SendForgeBlock(string account, long nonce, string startMsgId)
        {
            ForgeMsg forgeMsg = new ForgeMsg();
            forgeMsg.Account = account;
            forgeMsg.Nonce = nonce;
            forgeMsg.StartMsgId = startMsgId;
            string json = JsonConvert.SerializeObject(forgeMsg);
            RabbitMqClient.Current.ProduceMessage(RabbitMqName.ForgetBlock, MsgType.ForgetBlock, json);
        }

        /// <summary>
        /// 接口实例
        /// </summary>
        public static RabbitMQApi Current;

        /// <summary>
        /// 构造函数，RabbitMQ注册三种类型的消息，然后监听这三种类型的消息
        /// </summary>
        public RabbitMQApi()
        {
            try
            {
                //三种消息需要创建三个消息队列
                RabbitMqClient.Current.Regist(MsgType.StartMining, ReceiveStartMsg);
                RabbitMqClient.Current.Regist(MsgType.StopMining, ReceiveStopMsg);
                RabbitMqClient.Current.Regist(MsgType.Login, ReceiveLoginMsg);
                RabbitMqClient.Current.Listen();
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.ToString());
            }
        }

        static SafeCollection<string> ForgeJsons = new SafeCollection<string>();

        /// <summary>
        /// 接收开启命令
        /// 接收到的json反序列化StartMiningMsg对象，构造PoolTask类，添加到Pool任务中
        /// </summary>
        /// <param name="json"></param>
        public void ReceiveStartMsg(string json)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<StartMiningMsg>(json);

                if (msg == null)
                {
                    return;
                }

                LogHelper.Info("Receive StartMsg");

                PoolTask poolTask = new PoolTask();

                poolTask.CurrentBlockHeight = msg.BlockHeight;
                poolTask.CurrentScoopNumber = msg.ScoopNumber;
                poolTask.CurrentStartMsg = msg.GetStartMsg();
                poolTask.GeneratingBlock = RedisManager.Current.GetDataInRedis<BlockMsg>(msg.Id.ToString());
                poolTask.BaseTarget = msg.BaseTarget;
                poolTask.StartTime = msg.StartTime;
                poolTask.State = MiningState.Wait;
                poolTask.Id = msg.Id;

                PoolCache.poolTasks.Clear();
                PoolCache.poolTasks.Add(poolTask);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.ToString());
                return;
            }
        }

        /// <summary>
        /// 接收停止命令
        /// 接收到的json反序列化StopMiningMsg对象，构造StopMsg类，遍历所有的矿工发送TCP停止命令
        /// </summary>
        /// <param name="json"></param>
        public void ReceiveStopMsg(string json)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<StopMiningMsg>(json);

                LogHelper.Info("Receive StopMsg");

                if (msg == null || PoolCache.CurrentTask == null)
                    return;

                StopMsg stopMsg = new StopMsg();
                stopMsg.Result = msg.StopReason == StopReason.MiningSucesses;
                stopMsg.BlockHeight = msg.CurrentHeight;
                stopMsg.StopTime = msg.StopTime;


                var miners = PoolCache.WorkingMiners.ToArray();
                foreach (Miner item in miners)
                {
                    try
                    {
                        TcpState tcpState = new TcpState() { Client = item.Client, Stream = item.Stream, Address = item.ClientAddress };
                        StopCommand.Send(tcpState, stopMsg);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error(ex.ToString());
                    }
                }

                var stopTask = PoolCache.CurrentTask;
                //计算每个账户的工作量
                if (PoolCache.Efforts.ContainsKey(stopTask.CurrentBlockHeight))
                {
                    var items = PoolCache.Efforts[stopTask.CurrentBlockHeight];
                    stopTask.MinerEfforts.ForEach(x =>
                    {
                        var item = items.FirstOrDefault(p => p.Account == x.Account);
                        if (item == null)
                        {
                            items.Add(new EffortInfo { Account = x.Account, Effort = x.Effort, BlockHeight = stopTask.CurrentBlockHeight });
                        }
                        else
                        {
                            item.Effort += x.Effort;
                        }
                    });
                }
                else
                {
                    var efforts = stopTask.MinerEfforts.Select(x => new EffortInfo { Account = x.Account, Effort = x.Effort, BlockHeight = stopTask.CurrentBlockHeight }).ToList();
                    PoolCache.Efforts.Add(stopTask.CurrentBlockHeight, efforts);
                }
                //成功挖到区块，工作量保存在redis中，清空以前区块的Task
                if (msg.StopReason == StopReason.MiningSucesses)
                {
                    TimerTasks.Current.SaveMinerEffortToRedis(msg.CurrentHeight);
                    PoolCache.poolTasks.RemoveAll(x => x.CurrentBlockHeight <= stopMsg.BlockHeight);
                }
                PoolCache.CurrentTask = null;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        public void ReceiveLoginMsg(string json)
        {
            var msg = JsonConvert.DeserializeObject<MinerLoginMsg>(json);

            if (msg == null)
                return;

            LogHelper.Info("Receive LoginMsg");

            if (msg.ServerId == Setting.PoolId)
                return;

            var miner = PoolCache.WorkingMiners.FirstOrDefault(x => x.SerialNo == msg.SN || x.WalletAddress == msg.Account);
            if (miner == null)
                return;

            TcpState tcpState = new TcpState() { Client = miner.Client, Stream = miner.Stream, Address = miner.ClientAddress };
            StopCommand.Send(tcpState, new StopMsg
            {
                BlockHeight = PoolCache.CurrentTask.CurrentBlockHeight,
                Result = false,
                StartTime = PoolCache.CurrentTask.StartTime,
                StopTime = Time.EpochTime
            });
            RejectCommand.Send(tcpState);
            PoolCache.WorkingMiners.Remove(miner);
        }

        /// <summary>
        /// 发送登录消息到RabbitMq
        /// </summary>
        /// <param name="msg"></param>
        public void SendLoginMsg(MinerLoginMsg msg)
        {
            var json = JsonConvert.SerializeObject(msg);
            RabbitMqClient.Current.ProduceMessage(RabbitMqName.Login, MsgType.Login, json);
        }
    }
}
