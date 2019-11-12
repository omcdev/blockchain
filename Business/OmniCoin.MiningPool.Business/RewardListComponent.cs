using EdjCase.JsonRpc.Client;
using EdjCase.JsonRpc.Core;
using OmniCoin.Consensus.Api;
using OmniCoin.MiningPool.Data;
using OmniCoin.MiningPool.Entities;
using OmniCoin.Tools;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace OmniCoin.MiningPool.Business
{
    public class RewardListComponent
    {
        public List<RewardList> GetAllReward()
        {
            RewardListDac dac = new RewardListDac();
            return dac.SelectAll();
        }

        public RewardList GetRewardById(long id)
        {
            RewardListDac dac = new RewardListDac();
            return dac.SelectById(id);
        }

        public RewardList GetRewardByHash(string hash)
        {
            RewardListDac dac = new RewardListDac();
            return dac.SelectByHash(hash);
        }

        public long GetBlockReward(string hash)
        {
            //调接口获取奖励
            AuthenticationHeaderValue authHeaderValue = null;
            RpcClient client = new RpcClient(new Uri(MiningPoolSetting.API_URI), authHeaderValue, null, null, "application/json");
            RpcRequest request = RpcRequest.WithParameterList("GetBlockReward", new List<object> { hash }, 1);
            RpcResponse response = client.SendRequestAsync(request).Result;
            if (response.HasError)
            {
                throw new ApiCustomException(response.Error.Code, response.Error.Message);
            }

            long totalReward = response.GetResult<long>();
            return totalReward;
        }

        public RewardList InsertRewardList(RewardList entity)
        {
            RewardListDac dac = new RewardListDac();
            //if (dac.IsExisted(entity.BlockHash))
            //{
            //    throw new Exception("block hash has existed");
            //}
            //调接口获取奖励
            //AuthenticationHeaderValue authHeaderValue = null;
            //RpcClient client = new RpcClient(new Uri(MiningPoolSetting.API_URI), authHeaderValue, null, null, "application/json");
            //RpcRequest request = RpcRequest.WithParameterList("GetBlockReward", new List<object> { entity.BlockHash }, 1);
            //RpcResponse response = await client.SendRequestAsync(request);
            //if (response.HasError)
            //{
            //    throw new ApiCustomException(response.Error.Code, response.Error.Message);
            //}
            
            //long totalReward = response.GetResult<long>();
            RewardList reward = new RewardList();
            
            AwardSetting setting = ConfigurationTool.GetAppSettings<AwardSetting>("OmniCoin.MiningPool.Business.conf.json", "AwardSetting");
            //double extractProportion = setting.ExtractProportion;
            //double serviceFeeProportion = setting.ServiceFeeProportion;
            reward.BlockHash = entity.BlockHash;
            reward.GenerateTime = entity.GenerateTime;
            reward.Hashes = entity.Hashes;
            reward.MinerAddress = entity.MinerAddress;
            reward.OriginalReward = entity.OriginalReward;
            //reward.ActualReward = Convert.ToInt64(entity.OriginalReward * extractProportion);
            reward.ActualReward = Convert.ToInt64(entity.OriginalReward);
            reward.Paid = 0;
            reward.PaidTime = Framework.Time.EpochStartTime.Millisecond;
            reward.IsCommissionProcessed = 0;
            reward.CommissionProcessedTime = 0;
            //此市transaction为“”，需要同步后才能写数据进去
            reward.TransactionHash = entity.TransactionHash;

            dac.Insert(reward);

            return reward;
        }

        public void UpdatePaidStatus(long id, string transactionHash)
        {
            RewardListDac dac = new RewardListDac();
            dac.UpdatePaid(id, 1, transactionHash);
        }

        public void UpdatePaidStatus(string tableName, string address, string transactionHash, string blockHashes)
        {
            RewardListDac dac = new RewardListDac();
            dac.UpdatePaid(tableName, address, 1, transactionHash, blockHashes);
        }       

        /*
        public void UpdatePaidStatus(string hash, int status)
        {
            RewardListDac dac = new RewardListDac();
            dac.UpdatePaid(hash, status);
        }
        */

        public void DeleteReward(long id)
        {
            RewardListDac dac = new RewardListDac();
            dac.Delete(id);
        }

        public void DeleteReward(string hash)
        {
            RewardListDac dac = new RewardListDac();
            dac.Delete(hash);
        }

        /// <summary>
        /// 获取单个矿工的未发放的奖励
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public long GetUnPaidReward(string address)
        {
            RewardListDac dac = new RewardListDac();
            return dac.GetUnPaidReward(address, 0);
        }

        /// <summary>
        /// 获取单个矿工的已发放的奖励
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public long GetPaidReward(string address)
        {
            RewardListDac dac = new RewardListDac();
            return dac.GetPaidReward(address, 1);
        }

        public List<RewardList> GetAllUnPaidReward(string tableName, string blockHashes)
        {
            RewardListDac dac = new RewardListDac();
            return dac.GetAllUnPaidReward(tableName, blockHashes);
        }        

        /// <summary>
        /// 固定地址的发奖励,测试发消息队列专用
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="blockHashes"></param>
        /// <returns></returns>
        public List<RewardList> GetAllUnPaidRewardByAddresses(string tableName, string blockHashes, string addresses)
        {
            RewardListDac dac = new RewardListDac();
            return dac.GetAllUnPaidRewardByAddresses(tableName, blockHashes, addresses);
        }

        public List<RewardList> GetUnPaidRewardBlock()
        {
            RewardListDac dac = new RewardListDac();
            return dac.GetUnPaidRewardBlock(); 
        }

        public List<RewardList> GetCustomUnPaidReward(int count)
        {
            RewardListDac dac = new RewardListDac();
            return dac.GetCustomUnPaidReward(count);
        }

        public long GetActualReward(string address, string blockHash)
        {
            RewardListDac dac = new RewardListDac();
            return dac.GetActualReward(address, blockHash);
        }

        /// <summary>
        /// 删除过期的table
        /// </summary>
        public void DropExpireTables()
        {
            new RewardListDac().DropExpireTables();
        }

        public void UpdatePaidStatusByAddresses(string tableName, string address, string transactionHash, string blockHashes)
        {
            RewardListDac dac = new RewardListDac();
            dac.UpdatePaidByAddresses(tableName, address, 1, transactionHash, blockHashes);
        }

        public void UpdateNullPaidStatus(string tableName, string address, string blockHashes)
        {
            RewardListDac dac = new RewardListDac();
            dac.UpdateNullPaid(tableName, address, 1, blockHashes);
        }

        public List<RewardList> GetAllUnPaidRewardGroup(string tableName, string blockHashes)
        {
            RewardListDac dac = new RewardListDac();
            Dictionary<string, long> dic = dac.GetAllUnPaidRewardGroup(tableName, blockHashes);
            List<RewardList> result = new List<RewardList>();
            foreach (var item in dic)
            {
                result.Add(new RewardList { MinerAddress = item.Key, ActualReward = item.Value, OriginalReward = item.Value });
            }
            return result;
        }

        /// <summary>
        /// 返回当前所有未到期的存币记录
        /// </summary>
        /// <returns></returns>
        public List<DepositList> GetAllNotExpiredDeposit()
        {
            return  new RewardListDac().GetAllNotExpiredDeposit();
        }

        /// <summary>
        /// 更新存币记录表 此刻已到期的存币记录的状态为已到期
        /// </summary>
        public void UpdateDepositStatus()
        {
            new RewardListDac().UpdateDepositStatus();
        }

        public void InsertDeposit(List<DepositList> deposits)
        {
            new RewardListDac().InsertDeposit(deposits);
        }
    }
}
