using OmniCoin.Consensus;
using OmniCoin.Consensus.Api;
using OmniCoin.Framework;
using OmniCoin.MiningPool.API.Config;
using OmniCoin.MiningPool.API.DataPools;
using OmniCoin.MiningPool.API.DTO;
using OmniCoin.MiningPool.Business;
using OmniCoin.MiningPool.Entities;
using OmniCoin.MiningPool.Shares;
using OmniCoin.Pool.Redis;
using OmniCoin.ShareModels.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OmniCoin.MiningPool.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class MinersController : BaseController
    {
        private IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        IHostingEnvironment env;
        public MinersController(IDistributedCache distributedCache, IHostingEnvironment _env)
        {
            _distributedCache = distributedCache;
            env = _env;
        }

        public static List<PoolInfo> Pools = new List<PoolInfo>
        {
            new PoolInfo{  MinerCount = 0, PoolAddress = "47.244.126.19",Port = 5009, PullTime = Time.EpochTime},
            new PoolInfo{  MinerCount = 0, PoolAddress = "47.244.130.138",Port = 5009, PullTime = Time.EpochTime},
            new PoolInfo{  MinerCount = 0, PoolAddress = "47.244.130.167",Port = 5009, PullTime = Time.EpochTime},
            new PoolInfo{  MinerCount = 0, PoolAddress = "47.244.59.23",Port = 5009, PullTime = Time.EpochTime},
            new PoolInfo{  MinerCount = 0, PoolAddress = "47.52.99.211",Port = 5009, PullTime = Time.EpochTime},
            new PoolInfo{  MinerCount = 0, PoolAddress = "47.244.132.129",Port = 5009, PullTime = Time.EpochTime}
        };

        private volatile static int _index;

        private readonly object _lock = new object();

        const int MaxNonceTimeout = 60 * 60 * 2;//2hour
        const int ScoopNumberTimeout = 120;//30 seconds
        const int MaxNonceLimit = 131072;

        /// <summary>
        /// Pos信息校验
        /// </summary>
        /// <param name="miners"></param>
        /// <returns></returns>
        /// 
        [Obsolete]
        [HttpPost]
        public CommonResponse POSValidate([FromBody]Miners miners)
        {
            try
            {
                MinersComponent component = new MinersComponent();
                bool result = component.MinerLogin(miners.Address, miners.SN);
                return OK(result);
            }
            catch (ApiCustomException ce)
            {
                LogHelper.Error(ce.Message);
                return Error(ce.ErrorCode, ce.ErrorMessage);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
                return Error(ex.HResult, ex.Message);
            }
        }

        /// <summary>
        /// 保存矿工信息到数据库
        /// </summary>
        /// <param name="miners"></param>
        /// <returns></returns>
        [HttpPost]
        public CommonResponse SaveMiners([FromBody]Miners miners)
        {
            try
            {
                MinersComponent component = new MinersComponent();
                Miners entity = component.RegisterMiner(miners.Address, miners.Account, miners.SN);
                return OK(entity);
            }
            catch (ApiCustomException ce)
            {
                LogHelper.Error(ce.Message);
                return Error(ce.ErrorCode, ce.ErrorMessage);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
                return Error(ex.HResult, ex.Message);
            }
        }

        /// <summary>
        /// 解除SN和address绑定
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public CommonResponse PosSNUnbind([FromBody]Miners miners)
        {
            try
            {
                MinersComponent component = new MinersComponent();
                component.PosSNUnbind(miners.Address, miners.Account, miners.SN);
                return OK();
            }
            catch (ApiCustomException ce)
            {
                LogHelper.Error(ce.Message);
                return Error(ce.ErrorCode, ce.ErrorMessage);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
                return Error(ex.HResult, ex.Message);
            }
        }

        /// <summary>
        /// 根据地址获取Miner信息
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        [HttpGet]
        public CommonResponse GetMinerInfoByAddress([FromBody]string address)
        {
            try
            {
                MinersComponent component = new MinersComponent();
                Miners entity = component.GetMinerByAddress(address);
                return OK(entity);
            }
            catch (ApiCustomException ce)
            {
                LogHelper.Error(ce.Message);
                return Error(ce.ErrorCode, ce.ErrorMessage);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
                return Error(ex.HResult, ex.Message);
            }
        }

        /// <summary>
        /// 根据地址获取未付款的奖励信息
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        [HttpGet]
        public CommonResponse GetUnPaidReward(string address)
        {
            try
            {
                //if (Time.EpochTime - Startup.Pool_Miners_UpdateTime > 5 * 60 * 1000)
                //{
                //    Startup.Pool_Miners_UpdateTime = Time.EpochTime;
                //    MinersComponent minersComponent = new MinersComponent();
                //    Startup.Pool_Miners = minersComponent.GetAllMiners();
                //}
                //var cacheKey = "Pool_Miners";
                //var data = _distributedCache.GetString("Pool_Miners");
                //List<Miners> miners = null;
                //if (data == null)
                //{
                //    //LogHelper.Info("Write data into cache");
                //    MinersComponent minersComponent = new MinersComponent();
                //    miners = minersComponent.GetAllMiners();
                //    data = Newtonsoft.Json.JsonConvert.SerializeObject(miners);
                //    _distributedCache.SetString(cacheKey, data, new DistributedCacheEntryOptions()
                //                        .SetSlidingExpiration(TimeSpan.FromMinutes(5)));
                //}
                //else
                //{
                //    miners = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Miners>>(data);
                //    //LogHelper.Info("Load data from cache");
                //}

                long result = 0;

                if(MinersJob.Current.Pool_Miners != null && MinersJob.Current.Pool_Miners.Count > 0)
                {
                    var miner = MinersJob.Current.Pool_Miners.FirstOrDefault(m => m.Address == address);

                    if(miner != null)
                    {
                        result = miner.UnpaidReward;
                    }
                }

                return OK(result);
            }
            catch (ApiCustomException ce)
            {
                LogHelper.Error(ce.Message, ce);
                return Error(ce.ErrorCode, ce.ErrorMessage);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
                return Error(ex.HResult, ex.Message);
            }
        }

        /// <summary>
        /// 根据地址获取已付款的奖励信息
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        [HttpGet]
        public CommonResponse GetPaidReward(string address)
        {
            try
            {
                //if (Time.EpochTime - Startup.Pool_Miners_UpdateTime > 5 * 60 * 1000)
                //{
                //    Startup.Pool_Miners_UpdateTime = Time.EpochTime;
                //    MinersComponent minersComponent = new MinersComponent();
                //    Startup.Pool_Miners = minersComponent.GetAllMiners();
                //}
                //var cacheKey = "Pool_Miners";
                //var data = _distributedCache.GetString("Pool_Miners");
                //List<Miners> miners = null;
                //if (data == null)
                //{
                //    //LogHelper.Info("Write data into cache");
                //    MinersComponent minersComponent = new MinersComponent();
                //    miners = minersComponent.GetAllMiners();
                //    data = Newtonsoft.Json.JsonConvert.SerializeObject(miners);
                //    _distributedCache.SetString(cacheKey, data, new DistributedCacheEntryOptions()
                //                        .SetSlidingExpiration(TimeSpan.FromMinutes(5)));
                //}
                //else
                //{
                //    miners = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Miners>>(data);
                //    //LogHelper.Info("Load data from cache");
                //}

                long result = 0;

                if (MinersJob.Current.Pool_Miners != null && MinersJob.Current.Pool_Miners.Count > 0)
                {
                    var miner = MinersJob.Current.Pool_Miners.FirstOrDefault(m => m.Address == address);

                    if (miner != null)
                    {
                        result = miner.PaidReward;
                    }
                }

                return OK(result);
            }
            catch (ApiCustomException ce)
            {
                LogHelper.Error(ce.Message, ce);
                return Error(ce.ErrorCode, ce.ErrorMessage);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
                return Error(ex.HResult, ex.Message);
            }
        }

        /// <summary>
        /// 获取所有的矿工信息
        /// </summary>
        /// <returns></returns>
        [Obsolete]
        [HttpGet]
        public CommonResponse GetAllMiners()
        {
            try
            {
                List<string> result = null;
                if (Startup.MinerListAction == null)
                    result = null;
                else
                    result = Startup.MinerListAction();
                return OK(result);
            }
            catch (ApiCustomException ce)
            {
                LogHelper.Error(ce.Message, ce);
                return Error(ce.ErrorCode, ce.ErrorMessage);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
                return Error(ex.HResult, ex.Message);
            }
        }

        /// <summary>
        /// 获取单个服务器信息
        /// </summary>
        /// <returns></returns>
        [Obsolete]
        [HttpGet]
        public CommonResponse GetServer()
        {
            lock (_lock)
            {
                try
                {
                    /* 1、组织配置文件，配置文件里面是验证服务器的Name和IP
                     * 2、从配置文件中获取Name，按照一定的规则组成key，然后根据Key从redis中获取服务器在线矿工数量以及数据更新时间
                     * 3、从redis中获取矿工人数最少的服务器（排除掉线的服务器---更新时间超过规定时间的服务器）
                     * 4、如果获取的矿工人数最少的服务器的矿工数目大于规定数目则返回null，否则返回服务器IP地址
                     * 配置参数：数据更新时间，矿工人数限制
                     * 
                     */

                    //var pools = ServerPool.Default.Pools.ToList();
                    //pools = pools.Where(x => !x.PoolAddress.StartsWith("127")).ToList();
                    //var minCount = pools.Min(x => x.MinerCount);
                    //if (minCount < ServerPool.Default.MinerAmount)
                    //{
                    //    var result = pools.FirstOrDefault(x => x.MinerCount == minCount);
                    //    if (result != null)
                    //        return OK(new { IPAddress = result.PoolAddress, Name = result.PoolId, Port = result.Port });
                    //}

                    //var pools = ServerPool.Default.Pools.ToList();
                    var pools = Pools.Where(x => !x.PoolAddress.StartsWith("127")).ToList();

                    //LogHelper.Info("Pool count " + pools.Count);
                    //LogHelper.Info("Server index " + _index);

                    if (pools.Count == 0) return OK();

                    if (_index >= pools.Count)
                        _index = 0;

                    var result = pools[_index];
                    
                    _index++;                    
                    return OK(new { IPAddress = result.PoolAddress, Name = result.PoolId, Port = result.Port });

                }
                catch (ApiCustomException ce)
                {
                    LogHelper.Error(ce.Message, ce);
                    return Error(ce.ErrorCode, ce.ErrorMessage);
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex.Message, ex);
                    return Error(ex.HResult, ex.Message);
                }
            }
        }

        /// <summary>
        /// 获取服务器列表信息
        /// </summary>
        /// <returns></returns>
        [Obsolete]
        [HttpGet]
        public CommonResponse GetServerList()
        {
            try
            {
                var result = Pools.Select(x => new ServerInfo { IPAddress = x.PoolAddress, MinerCount = x.MinerCount, Name = x.PoolId, port = x.Port }).ToList();

                return OK(result);
            }
            catch (ApiCustomException ce)
            {
                LogHelper.Error(ce.Message, ce);
                return Error(ce.ErrorCode, ce.ErrorMessage);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
                return Error(ex.HResult, ex.Message);
            }
        }

        /// <summary>
        /// 获取ScoopNumber
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        [HttpGet]
        public CommonResponse GetScoopNumber(string address)
        {
            try
            {
                try
                {
                    if (!AccountIdHelper.AddressVerify(address))
                    {
                        return Error(Entities.MiningPoolErrorCode.Miners.ADDRESS_IS_INVALID, $"Address {address} is invalid");
                    }
                }
                catch
                {
                    return Error(Entities.MiningPoolErrorCode.Miners.ADDRESS_IS_INVALID, $"Address {address} is invalid");
                }

                var miner = MinersJob.Current.Pool_Miners.FirstOrDefault(m => m.Address == address);

                if (miner == null)
                {
                    return Error(Entities.MiningPoolErrorCode.Miners.ADDRESS_NOT_EXIST, $"Address {address} is not exist");
                }

                var key = "Pool:MinerScoopNumber:" + address;
                var scoopNumber = RedisManager.Current.GetDataInRedis<string>(key);
                var ttl = RedisManager.Current.PTtl(key);

                if (string.IsNullOrWhiteSpace(scoopNumber) || ttl < 10 * 1000)
                {
                    var random = new Random();
                    scoopNumber = random.Next(0, 4096).ToString();
                    RedisManager.Current.SaveDataToRedis(key, scoopNumber.ToString(), ScoopNumberTimeout);
                }

                return OK(scoopNumber);
            }
            catch (ApiCustomException ce)
            {
                LogHelper.Error(ce.Message, ce);
                return Error(ce.ErrorCode, ce.ErrorMessage);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
                return Error(ex.HResult, ex.Message);
            }
        }

        /// <summary>
        /// 提交最大Nonce
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public CommonResponse SubmitMaxNonce([FromBody]MaxNonceIM model)
        {
            try
            {
                if (model == null)
                {
                    return Error(Entities.MiningPoolErrorCode.Miners.COMMON_ERROR, "Posted data error");
                }

                try
                {
                    LogHelper.Info($"Address is {model.Address}, network is {GlobalParameters.IsTestnet}");
                    if (!AccountIdHelper.AddressVerify(model.Address))
                    {
                        return Error(Entities.MiningPoolErrorCode.Miners.ADDRESS_IS_INVALID, $"Address {model.Address}is invalid");
                    }
                }
                catch
                {
                    return Error(Entities.MiningPoolErrorCode.Miners.ADDRESS_IS_INVALID, $"Address {model.Address} is invalid");
                }

                var miner = MinersJob.Current.Pool_Miners.FirstOrDefault(m => m.Address == model.Address);

                if (miner == null)
                {
                    return Error(Entities.MiningPoolErrorCode.Miners.ADDRESS_NOT_EXIST, $"Address {model.Address} not exist");
                }
                else if(miner.SN != model.SN)
                {
                    return Error(Entities.MiningPoolErrorCode.Miners.SN_CODE_ERROR, $"SN code {model.SN} with address {model.Address} is error");
                }

                var scoopNumberKey = "Pool:MinerScoopNumber:" + model.Address;
                var maxNonceKey = "Pool:MinerMaxNonce:" + model.Address;

                var scoopNumber = RedisManager.Current.GetDataInRedis<string>(scoopNumberKey);
                
                if(scoopNumber == null || scoopNumber != model.ScoopNumber.ToString())
                {
                    return Error(Entities.MiningPoolErrorCode.Miners.SCOOPNUMBER_NOT_MATCH, $"Scoop number from address {model.Address} SN {model.SN} is not matched: {scoopNumber}/{model.ScoopNumber}");
                }

                if (model.MaxNonce > MaxNonceLimit)
                {
                    return Error(Entities.MiningPoolErrorCode.Miners.MAXNONCE_IS_INVALID, $"Max nonce {model.MaxNonce} from address {model.Address} is invalid");
                }

                //var data = POC.CalculateScoopData(model.Address, model.MaxNonce, model.ScoopNumber);

                //try
                //{
                //    var scoopData = Convert.FromBase64String(model.ScoopData);

                //    if (Base16.Encode(data) != Base16.Encode(scoopData))
                //    {
                //        return Error(Entities.MiningPoolErrorCode.Miners.SCOOP_DATA_IS_INVALID, "Scoop data is invalid");
                //    }
                //}
                //catch
                //{
                //    return Error(Entities.MiningPoolErrorCode.Miners.SCOOP_DATA_IS_INVALID, "Scoop data is invalid");
                //}

                RedisManager.Current.SaveDataToRedis<string>(maxNonceKey, model.MaxNonce.ToString(), MaxNonceTimeout);
                return OK(true);
            }
            catch (ApiCustomException ce)
            {
                LogHelper.Error(ce.Message);
                return Error(ce.ErrorCode, ce.ErrorMessage);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
                return Error(ex.HResult, ex.Message);
            }
        }

    }

}