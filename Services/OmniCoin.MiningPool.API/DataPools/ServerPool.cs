using OmniCoin.Framework;
using OmniCoin.MiningPool.API.Config;
using OmniCoin.MiningPool.Shares;
using OmniCoin.Pool.Redis;
using OmniCoin.ShareModels.Models;
using OmniCoin.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace OmniCoin.MiningPool.API.DataPools
{
    public abstract class SingleBase<T>
    {
        private static T _default;
        public static T Default
        {
            get
            {
                if (_default == null)
                    _default = Activator.CreateInstance<T>();
                return _default;
            }
        }
    }

    public class ServerPool : SingleBase<ServerPool>
    {
        Timer updateServerTimer = null;
        bool isTestnet = false;
        bool isStart = false;
        public long MinerAmount = 500;
        public ServerPool()
        {
            
        }

        public void Start()
        {
            if (isStart)
                return;
            isStart = true;            
            var setting = ConfigurationTool.GetAppSettings<ServerSetting>("OmniCoin.MiningPool.API.conf.json", "ServerSetting");
            isTestnet = setting.IsTestNet;
            MinerAmount = setting.MinerAmount;
            updateServerTimer = new Timer();
            updateServerTimer.Elapsed += UpdateServerTimer_Elapsed;
            updateServerTimer.Interval = 1000;
            updateServerTimer.Start();
        }

        public void Stop()
        {
            updateServerTimer.Stop();
        }

        public SafeCollection<PoolInfo> Pools = new SafeCollection<PoolInfo>();

        private void UpdateServerTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            updateServerTimer.Stop();
            try
            {
                var poolIds = RedisManager.Current.GetDataInRedis<List<string>>(KeyHelper.GetPoolCenterName(isTestnet));
                if (poolIds == null || !poolIds.Any())
                {
                    Pools = new SafeCollection<PoolInfo>();
                    LogHelper.Info("poolIds is null");
                    return;
                }
                var poolInfoKeys = poolIds.Select(x => KeyHelper.GetPoolInfoKey(x));
                if (poolInfoKeys == null || !poolInfoKeys.Any())
                {
                    Pools = new SafeCollection<PoolInfo>();
                    LogHelper.Info("poolInfoKeys is null");
                    return;
                }
                var serverList = poolInfoKeys.Select(x => RedisManager.Current.GetDataInRedis<PoolInfo>(x)).ToList();
                if (poolInfoKeys == null || !serverList.Any())
                {
                    Pools = new SafeCollection<PoolInfo>();
                    LogHelper.Info("serverList is null");
                    return;
                }

                Pools = serverList;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.ToString());
            }
            finally
            {
                updateServerTimer.Start();
            }
        }
    }
}
