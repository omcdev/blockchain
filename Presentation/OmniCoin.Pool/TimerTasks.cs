


using OmniCoin.Framework;
using OmniCoin.MiningPool.Shares;
using OmniCoin.Pool.Redis;
using OmniCoin.RabbitMQ;
using OmniCoin.ShareModels;
using OmniCoin.ShareModels.Models;
using OmniCoin.ShareModels.Msgs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Timers;

namespace OmniCoin.Pool
{
    public class TimerTasks
    {
        private static TimerTasks _current;
        public static TimerTasks Current
        {
            get
            {
                if (_current == null)
                    _current = new TimerTasks();
                return _current;
            }
        }

        public void Init()
        {
            const int updatePoolsTime = 1000 * 10;//10秒
            var updatePoolsTimer = new Timer(updatePoolsTime);
            updatePoolsTimer.AutoReset = true;
            updatePoolsTimer.Elapsed += UpdatePoolsTimer_Elapsed;
            updatePoolsTimer.Start();

            var heartTimer = new Timer(Setting.HEART_TIME);
            heartTimer.Elapsed += HeartTimer_Elapsed;
            heartTimer.Start();
        }

        /// <summary>
        /// 心跳包定时任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HeartTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                PoolHeartMsg msg = new PoolHeartMsg { HeartTime = Time.EpochTime, PoolId = Setting.PoolId };
                var json = JsonConvert.SerializeObject(msg);
                //MqManager.Current.Send(MsgType.HeartPool, json);
                RabbitMqClient.Current.ProduceMessage(RabbitMqName.HeartPool, MsgType.HeartPool, json);
            }
            catch (Exception ex)
            {
                LogHelper.Error("Error on HeartTimer_Elapsed", ex);
            }
        }

        /// <summary>
        /// 更新Pool数据定时任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdatePoolsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SavePoolInfoToRedis();
            SavePoolWorkingInfoToRedis();
        }

        private long uploadHash = 0;
        const int LogoutTime = 1000 * 60;

        /// <summary>
        /// 把Pool工作数据保存在redis中
        /// </summary>
        public void SavePoolWorkingInfoToRedis()
        {
            try
            {
                var key = KeyHelper.GetPoolWorkingInfoKey(Setting.PoolId);

                if (PoolCache.CurrentTask == null)
                    return;

                var totalEffort = PoolCache.CurrentTask.MinerEfforts.Sum(x => x.Effort);
                if (PoolCache.Efforts.ContainsKey(PoolCache.CurrentTask.CurrentBlockHeight))
                {
                    totalEffort += Convert.ToInt32(PoolCache.Efforts[PoolCache.CurrentTask.CurrentBlockHeight].Sum(x => x.Effort));
                }

                PoolWorkingInfo poolInfo = new PoolWorkingInfo
                {
                    HashRates = totalEffort - uploadHash,
                    Miners = PoolCache.WorkingMiners.Select(x => x.WalletAddress).ToArray(),
                    PushTime = Time.EpochTime,
                };
                uploadHash = totalEffort;
                RedisManager.Current.SaveDataToRedis(key, poolInfo);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.ToString());
            }
        }

        /// <summary>
        /// 把Pool信息保存redis
        /// </summary>
        public void SavePoolInfoToRedis()
        {
            try
            {

                uploadHash = 0;
                var key = KeyHelper.GetPoolInfoKey(Setting.PoolId);
                PoolInfo poolInfo = new PoolInfo
                {
                    MinerCount = PoolCache.WorkingMiners.Count(),
                    Port = Setting.PoolPort,
                    PullTime = Time.EpochTime,
                    PoolId = Setting.PoolId,
                    PoolAddress = Setting.PoolAddress
                };

                RedisManager.Current.SaveDataToRedis(key, poolInfo);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.ToString());
            }
        }
        
        /// <summary>
        /// 保存工作量到redis
        /// </summary>
        /// <param name="height"></param>
        public void SaveMinerEffortToRedis(int height)
        {
            try
            {
                //key格式$"{id}_MAIN_EFFORT_{height}"
                var key = KeyHelper.GetMinerEffortKey(Setting.PoolId, height);

                if (PoolCache.Efforts.ContainsKey(height))
                {
                    LogHelper.Debug("total reward = " + PoolCache.Efforts[height].Count);
                    List<EffortInfo> infos = PoolCache.Efforts[height];
                    if (infos == null)
                        infos = new List<EffortInfo>();

                    infos.ForEach(x => x.Effort = 131072);

                    RedisManager.Current.SaveDataToRedis(key, infos);
                }
                else
                {
                    LogHelper.Debug("Efforts = NULL");
                    List<EffortInfo> infos = new List<EffortInfo>();
                    RedisManager.Current.SaveDataToRedis(key, infos);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.ToString());
            }
        }

        private string GetExtenalIpAddress()
        {
            string IP = null;
            try
            {
                //从网址中获取本机ip数据  
                WebClient client = new System.Net.WebClient();
                client.Encoding = System.Text.Encoding.Default;
                string str = client.DownloadString("http://1111.ip138.com/ic.asp");
                client.Dispose();

                //提取外网ip数据 [218.104.71.178]  
                int i1 = str.IndexOf("["), i2 = str.IndexOf("]");
                IP = str.Substring(i1 + 1, i2 - 1 - i1);
            }
            catch (Exception) { }

            return IP;
        }
    }
}
