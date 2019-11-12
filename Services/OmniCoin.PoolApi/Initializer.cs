using FiiiChain.Framework;
using FiiiChain.MiningPool.Shares;
using FiiiChain.Pool.Redis;
using FiiiChain.PoolApi.Controllers;
using FiiiChain.PoolMessages;
using FiiiChain.RabbitMQ;
using FiiiChain.ShareModels;
using FiiiChain.ShareModels.Models;
using FiiiChain.ShareModels.Msgs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FiiiChain.PoolApi
{
    public class Initializer
    {
        public void Init()
        {
            //KafkaMQ.MqManager.Current.Regist(MsgType.StopMining, ReceiveStopMsg);
            RabbitMqClient.Current.Regist(MsgType.StopMining, ReceiveStopMsg);
        }

        public string ID = (new Guid()).ToString();

        public List<Miner> miners = new List<Miner>();

        public void ReceiveStopMsg(string json)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<StopMiningMsg>(json);

                LogHelper.Info("Receive StopMsg");

                if (this.miners == null || !this.miners.Any())
                    return;

                StopMsg stopMsg = new StopMsg();
                stopMsg.Result = msg.StopReason == StopReason.MiningSucesses;
                if (stopMsg.Result)
                {
                    var height = Convert.ToInt32(stopMsg.BlockHeight);
                    var key = KeyHelper.GetMinerEffortKey(ID, height);

                    var efforts = this.miners.Select(x => new EffortInfo { Account = x.Address, BlockHeight = height, Effort = x.MaxNonce }).ToList();
                    RedisManager.Current.SaveDataToRedis(key, efforts);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.ToString());
            }
        }
    }
}