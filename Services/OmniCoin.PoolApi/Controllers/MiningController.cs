using FiiiChain.Consensus;
using FiiiChain.Framework;
using FiiiChain.Pool.Redis;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FiiiChain.PoolApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MiningController : Controller
    {
        const int timeout = 1000 * 60 * 60 * 2;//2hour
        const int MaxNonce = 131072;


        // POST api/values
        [HttpPost]
        public void Post()
        {
            //[FromBody]
        Miner miner = null;
            if (miner == null)
                return;
            #region 校验
            //if (miner.MaxNonce > MaxNonce)
            //    return;

            //var data = POC.CalculateScoopData(miner.Address, miner.MaxNonce, miner.ScoopNumber);

            //var scoopData = Convert.FromBase64String(miner.ScoopData);

            //if (Base16.Encode(data) != Base16.Encode(scoopData))
            //    return;
            #endregion
            
            var key = "Pool:Miners:" + miner.Address;
            RedisManager.Current.SaveDataToRedis(key, miner.MaxNonce, timeout);
        }

        
    }

    public class Miner
    {
        public string SN;
        public string Address;
        public int MaxNonce;
        public string ScoopData;//Base64
        public int ScoopNumber;
    }
}