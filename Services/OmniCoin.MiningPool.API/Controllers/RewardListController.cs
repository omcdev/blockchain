//using OmniCoin.Consensus.Api;
//using OmniCoin.Framework;
//using OmniCoin.MiningPool.Business;
//using OmniCoin.MiningPool.Entities;
//using Microsoft.AspNetCore.Mvc;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace OmniCoin.MiningPool.API.Controllers
//{
//    [Obsolete]
//    [Route("api/[controller]/[action]")]
//    [ApiController]
//    public class RewardListController : BaseController
//    {
//        /// <summary>
//        /// 保存奖励信息到数据库
//        /// </summary>
//        /// <param name="entity"></param>
//        /// <returns></returns>
//        [HttpPost]
//        public  CommonResponse SaveReward([FromBody]RewardList entity)
//        {
//            try
//            {
//                RewardListComponent component = new RewardListComponent();
//                component.InsertRewardList(entity);
//                return OK();
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

//        /// <summary>
//        /// 根据地址和区块哈希获取实际的奖励信息
//        /// </summary>
//        /// <param name="address"></param>
//        /// <param name="blockHash"></param>
//        /// <returns></returns>
//        /// 
//        [HttpGet]
//        public CommonResponse GetActualReward(string address, string blockHash)
//        {
//            try
//            {
//                RewardListComponent component = new RewardListComponent();
//                long result = component.GetActualReward(address, blockHash);
//                return OK(result);
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
//    }
//}
