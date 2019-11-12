//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using OmniCoin.Consensus.Api;
//using OmniCoin.Framework;
//using OmniCoin.MiningPool.API.DataPools;
//using OmniCoin.MiningPool.Business;
//using OmniCoin.MiningPool.Entities;
//using OmniCoin.ShareModels.Models;
//using Microsoft.AspNetCore.Mvc;

//namespace OmniCoin.MiningPool.API.Controllers
//{
//    [Route("api/[controller]/[action]")]
//    [ApiController]
//    public class NewMinersController : BaseController
//    {
//        private readonly object _lock = new object();

//        /// <summary>
//        /// 矿工注册
//        /// </summary>
//        /// <returns></returns>
//        public CommonResponse Register([FromBody]Miners miners)
//        {
//            try
//            {
//                MinersComponent component = new MinersComponent();
//                Miners entity = component.RegisterMiner(miners.Address, miners.Account, miners.SN);
//                return OK(entity);
//            }
//            catch (ApiCustomException ce)
//            {
//                LogHelper.Error(ce.Message);
//                return Error(ce.ErrorCode, ce.ErrorMessage);
//            }
//            catch (Exception ex)
//            {
//                LogHelper.Error(ex.Message, ex);
//                return Error(ex.HResult, ex.Message);
//            }
//        }

//        public CommonResponse GetSuitablePoolInfo()
//        {
//            lock (_lock)
//            {
//                try
//                {
//                    /* 1、组织配置文件，配置文件里面是验证服务器的Name和IP
//                     * 2、从配置文件中获取Name，按照一定的规则组成key，然后根据Key从redis中获取服务器在线矿工数量以及数据更新时间
//                     * 3、从redis中获取矿工人数最少的服务器（排除掉线的服务器---更新时间超过规定时间的服务器）
//                     * 4、如果获取的矿工人数最少的服务器的矿工数目大于规定数目则返回null，否则返回服务器IP地址
//                     * 配置参数：数据更新时间，矿工人数限制
//                     * 
//                     */

//                    List<PoolInfo> pools = ServerPool.Default.Pools.ToList();
//                    if (pools.Count == 0)
//                    {
//                        return OK();
//                    }
//                    pools = pools.Where(x => !x.PoolAddress.StartsWith("127")).ToList();
//                    long minCount = pools.Min(x => x.MinerCount);
//                    if (minCount < ServerPool.Default.MinerAmount)
//                    {
//                        var result = pools.FirstOrDefault(x => x.MinerCount == minCount);
//                        if (result != null)
//                        {
//                            return OK(result);
//                        }
//                    }
//                    return Error(Entities.MiningPoolErrorCode.Miners.GET_POOL_INFO_ERROR, "get pool info failue");
//                }
//                catch (ApiCustomException ce)
//                {
//                    LogHelper.Error(ce.Message, ce);
//                    return Error(ce.ErrorCode, ce.ErrorMessage);
//                }
//                catch (Exception ex)
//                {
//                    LogHelper.Error(ex.Message, ex);
//                    return Error(ex.HResult, ex.Message);
//                }
//            }
//        }
//    }
//}