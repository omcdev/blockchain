


using OmniCoin.Framework;
using OmniCoin.MiningPool.Business;
using OmniCoin.MiningPool.Shares;
using OmniCoin.Pool.Redis;
using OmniCoin.PoolCenter.Apis;
using OmniCoin.ShareModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace OmniCoin.PoolCenter
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
            const int updatePoolsTime = 1000 * 60;//1 Min
            var updatePoolsTimer = new Timer(updatePoolsTime);
            updatePoolsTimer.AutoReset = true;
            updatePoolsTimer.Elapsed += UpdatePoolsTimer_Elapsed; ;
            updatePoolsTimer.Start();

            const int uploadHashsTime = 10 * 1000 * 60;//10 Min
            var uploadHashsTimer = new Timer(uploadHashsTime);
            uploadHashsTimer.AutoReset = true;
            uploadHashsTimer.Elapsed += UploadHashsTimer_Elapsed; ;
            uploadHashsTimer.Start();

            const int updateBlockRateTime = 1000 * 60 * 60; //1 Hour
            var updateBlockRateTimer = new Timer(updateBlockRateTime);
            updateBlockRateTimer.AutoReset = true;
            updateBlockRateTimer.Elapsed += UpdateBlockRateTimer_Elapsed;
            updateBlockRateTimer.Start();
        }

        private void UpdateBlockRateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                PoolApi.SaveBlockRates();
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.ToString());
            }
        }

        private void UploadHashsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                PoolApi.SaveHashRates();
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.ToString());
            }
        }

        private void UpdatePoolsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var timer = sender as Timer;
            try
            {
                SavePoolsToRedis(timer);
                BlocksComponent component = new BlocksComponent();
                component.GetVerifiedHashes();
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.ToString());
            }
        }

        public void SavePoolsToRedis(Timer timer)
        {
            timer.Stop();
            try
            {
                var key = KeyHelper.GetPoolCenterName(GlobalParameters.IsTestnet);
                var pools = CenterCache.Pools.Where(x => Time.EpochTime - x.Value < Setting.MAX_HEART_TIME).Select(x => x.Key).ToList();
                RedisManager.Current.SaveDataToRedis(key, pools);
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
    }
}