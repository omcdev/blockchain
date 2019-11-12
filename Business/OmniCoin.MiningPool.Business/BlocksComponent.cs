using EdjCase.JsonRpc.Client;
using EdjCase.JsonRpc.Core;
using OmniCoin.Consensus.Api;
using OmniCoin.Entities;
using OmniCoin.Framework;
using OmniCoin.MiningPool.Data;
using OmniCoin.MiningPool.Entities;
using OmniCoin.Tools;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace OmniCoin.MiningPool.Business
{
    public class BlocksComponent
    {
        //public List<Blocks> GetAllBlocks()
        //{
        //    BlocksDac dac = new BlocksDac();
        //    return dac.SelectAll();
        //}

        //public Blocks GetBlockById(long id)
        //{
        //    BlocksDac dac = new BlocksDac();
        //    return dac.SelectById(id);
        //}

        //public Blocks GetBlockByHash(string hash)
        //{
        //    BlocksDac dac = new BlocksDac();
        //    return dac.SelectByHash(hash);
        //}

        //public Blocks SaveBlock(Blocks entity)
        //{
        //    BlocksDac dac = new BlocksDac();
        //    Blocks block = new Blocks();
        //    List<RewardList> list = new List<RewardList>();
        //    //组织Block数据
        //    DateTime now = Time.GetLocalDateTime(entity.Timstamp);
        //    block.Confirmed = 0;
        //    block.Generator = entity.Generator;
        //    block.Hash = entity.Hash;
        //    block.Height = entity.Height;
        //    block.Nonce = entity.Nonce;
        //    block.Timstamp = entity.Timstamp;
        //    block.TotalHash = entity.TotalHash;
        //    block.TotalReward = entity.TotalReward;
        //    //组织RewradList数据
        //    ConfigurationTool tool = new ConfigurationTool();
        //    AwardSetting setting = tool.GetAppSettings<AwardSetting>("AwardSetting");
        //    double extractProportion = setting.ExtractProportion;
        //    double serviceFeeProportion = setting.ServiceFeeProportion;
        //    dac.Insert(block, list, now);
        //    return block;
        //}

        public Blocks SaveBlockAndRewardLists(Blocks entity, List<RewardList> rewardLists)
        {
            BlocksDac dac = new BlocksDac();
            Blocks block = new Blocks();
            List<RewardList> list = new List<RewardList>();
            //组织Block数据
            long epoTime = Time.EpochTime;
            DateTime now = Time.GetLocalDateTime(epoTime);
            block.Confirmed = 0;
            block.Generator = entity.Generator;
            block.Hash = entity.Hash;
            block.Height = entity.Height;
            block.Nonce = entity.Nonce;
            block.Timstamp = epoTime;
            block.TotalHash = entity.TotalHash;
            block.TotalReward = entity.TotalReward;
            block.IsRewardSend = 0;
            //组织RewradList数据            
            foreach (var item in rewardLists)
            {
                RewardList reward = new RewardList();
                reward.BlockHash = item.BlockHash;
                reward.GenerateTime = item.GenerateTime;
                reward.Hashes = item.Hashes;
                reward.MinerAddress = item.MinerAddress;
                reward.OriginalReward = item.OriginalReward;                
                reward.ActualReward = item.ActualReward;
                reward.Paid = 0;
                reward.PaidTime = Time.EpochStartTime.Millisecond;
                reward.IsCommissionProcessed = 0;
                reward.CommissionProcessedTime = 0;
                //此处transaction为""，需要奖励真正发放后才会更新奖励的交易Hash
                reward.TransactionHash = "";
                reward.RewardType = item.RewardType;
                reward.DepositTotalAmount = item.DepositTotalAmount;
                reward.AddressDepositTotalAmount = item.AddressDepositTotalAmount;
                reward.DepositTransactionHash = item.DepositTransactionHash;
                list.Add(reward);
            }
            dac.Insert(block, list, now);
            return block;
        }

        //public Blocks SaveBlockAndRewardListsSourceBackup(Blocks entity, List<RewardList> rewardLists)
        //{
        //    BlocksDac dac = new BlocksDac();
        //    Blocks block = new Blocks();
        //    List<RewardList> list = new List<RewardList>();
        //    //组织Block数据
        //    long epoTime = Time.EpochTime;
        //    DateTime now = Time.GetLocalDateTime(epoTime);
        //    block.Confirmed = 0;
        //    block.Generator = entity.Generator;
        //    block.Hash = entity.Hash;
        //    block.Height = entity.Height;
        //    block.Nonce = entity.Nonce;
        //    block.Timstamp = epoTime;
        //    block.TotalHash = entity.TotalHash;
        //    block.TotalReward = entity.TotalReward;
        //    block.IsRewardSend = 0;
        //    //组织RewradList数据
        //    ConfigurationTool tool = new ConfigurationTool();
        //    AwardSetting setting = tool.GetAppSettings<AwardSetting>("AwardSetting");
        //    double extractProportion = setting.ExtractProportion;
        //    double serviceFeeProportion = setting.ServiceFeeProportion;
        //    foreach (var item in rewardLists)
        //    {
        //        RewardList reward = new RewardList();
        //        reward.BlockHash = item.BlockHash;
        //        reward.GenerateTime = item.GenerateTime;
        //        reward.Hashes = item.Hashes;
        //        reward.MinerAddress = item.MinerAddress;
        //        reward.OriginalReward = item.OriginalReward;
        //        reward.ActualReward = Convert.ToInt64(item.OriginalReward * extractProportion);
        //        reward.Paid = 0;
        //        reward.PaidTime = Time.EpochStartTime.Millisecond;
        //        reward.IsCommissionProcessed = 0;
        //        reward.CommissionProcessedTime = 0;
        //        //此处transaction为""，需要同步后才能写数据进去
        //        reward.TransactionHash = "";

        //        list.Add(reward);
        //    }
        //    dac.Insert(block, list, now);
        //    return block;
        //}

        //public void UpdateBlockConfirmed(long id, int confirmed, int isDiacarded)
        //{
        //    BlocksDac dac = new BlocksDac();
        //    dac.UpdateConfirmed(id, confirmed, isDiacarded);
        //}

        public void UpdateBlockConfirmed(string hash, int confirmed, int isDiacarded)
        {
            BlocksDac dac = new BlocksDac();
            dac.UpdateConfirmed(hash, confirmed, isDiacarded);
        }

        //public void DeleteBlock(long id)
        //{
        //    BlocksDac dac = new BlocksDac();
        //    dac.Delete(id);
        //}

        //public void DeleteBlock(string hash)
        //{
        //    BlocksDac dac = new BlocksDac();
        //    dac.Delete(hash);
        //}

        public void UpdateFailBlock(long height)
        {
            BlocksDac dac = new BlocksDac();
            dac.UpdateFailBlock(height);
        }


        /* 实现思路
         * 1、调取JsonRpc接口获取当前区块高度
         * 2、根据当前区块高度排除数据库中6个以内的区块（因为一定是未确认的）
         * 3、拿出剩余未确认的区块，调取Rpc接口判断区块是否被确认
         * 4、批量更新数据库的确认状态和是否作废状态
         * 5、需要更新RewardList表中的是否作废状态
         * 
         * 备注：Rpc接口判断区块是否被确认这个接口需要自己用Rpc写
         * 接口：根据传入的区块Hash判断是否区块是否被确认
         * 接口返回值：返回被确认的区块Hash
         */

        /// <summary>
        /// 更新区块的确认状态和抛弃状态
        /// </summary>
        /// <returns></returns>
        public async Task GetVerifiedHashes()
        {
            //不能直接调用OmniCoin.Bussiness，需要使用JsonRpc调用接口
            //先通过JsonRpc获取当前区块高度
            LogHelper.Debug($"****************begin to sync blocks********************");
            BlocksDac dac = new BlocksDac();
            RewardListDac rewardDac = new RewardListDac();
            MinersDac minersDac = new MinersDac();
            AuthenticationHeaderValue authHeaderValue = null;
            LogHelper.Debug($"API_URI is {MiningPoolSetting.API_URI}");
            long responseValue = 0;
            try
            {
                RpcClient client = new RpcClient(new Uri(MiningPoolSetting.API_URI), authHeaderValue, null, null, "application/json");
                RpcRequest request = RpcRequest.WithNoParameters("GetBlockCount", 1);
                RpcResponse response = await client.SendRequestAsync(request);
                if (response.HasError)
                {
                    throw new ApiCustomException(response.Error.Code, response.Error.Message);
                }

                responseValue = response.GetResult<long>();
                LogHelper.Debug($"responseValue:{responseValue}");
                LogHelper.Debug($"sqlite block hight is {responseValue}");
                if (responseValue - 100 > 0)
                {
                    //根据responseValue获取数据库中高度小于responseValue - 6的所有Hash值
                    List<string> hashes = dac.GetAppointedHash(responseValue - 100);
                    RpcRequest requestHash = RpcRequest.WithParameterList("GetVerifiedHashes", new List<object> { hashes }, 1);
                    RpcResponse responseHash = await client.SendRequestAsync(requestHash);
                    if (responseHash.HasError)
                    {
                        throw new ApiCustomException(response.Error.Code, response.Error.Message);
                    }
                    List<Block> list = responseHash.GetResult<List<Block>>();
                    LogHelper.Info($"Verified Hashes count is {list.Count}");
                    /*发送到阿里云消息队列
                    foreach (Block item in list)
                    {
                        //根据Block获取RewardList
                        string tableName = "RewardList" + Time.GetLocalDateTime(item.Timestamp).ToString("yyyyMMdd");
                        List<RewardList> rewardList = rewardDac.GetListByHash(tableName, item.Hash);
                        string sendBody = Newtonsoft.Json.JsonConvert.SerializeObject(new { item, rewardList });

                        AliMQ.ProducerMessage producer = new AliMQ.ProducerMessage();
                        producer.Initialize("MinerReward");
                        producer.SendNormalMessage(item.GeneratorId, sendBody, item.Hash);
                    }
                    */
                    //根据list的值批量更新数据库
                    foreach (var item in list)
                    {
                        LogHelper.Info($"begin update block confirm");
                        UpdateBlockConfirmed(item.Hash, (item.IsVerified ? 1 : 0), (item.IsDiscarded ? 1 : 0));
                        string tableName = "RewardList" + Time.GetLocalDateTime(item.Timestamp).ToString("yyyyMMdd");
                        //如果区块作废就更新RewardList表状态
                        if (item.IsDiscarded)
                        {
                            LogHelper.Info($"begin update discarded blocks");
                            rewardDac.UpdatePaid(tableName, item.Hash, 2, responseValue - 100);
                            //更新Miners表中的未发放UnpaidReward余额
                            minersDac.UpdateDiscardedUnpaidReward(tableName, item.Hash);
                        }
                    }
                    //丢弃那些状态失败的，根据区块高度和confirm=0更新IsDiscard
                    UpdateFailBlock(responseValue - 100);
                    LogHelper.Debug($"****************end to sync blocks********************");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
            }
        }

        //public void GetTimerVerifiedHashes()
        //{
        //    //定时调用接口
        //    System.Timers.Timer timer = new System.Timers.Timer();
        //    timer.AutoReset = true;
        //    timer.Enabled = true;
        //    timer.Interval = 30 * 60 * 1000;
        //    timer.Elapsed += async (sender, e) =>
        //    {
        //        //调用GetVerifiedHashes方法
        //        await GetVerifiedHashes();
        //    };
        //}

        public List<Blocks> GetAllUnRewardBlocks(long? timestamp)
        {
            return new BlocksDac().GetAllUnRewardBlocks(timestamp);
        }

        public void UpdateAllSendStatus(string tableName, string blockHashes)
        {
            new BlocksDac().UpdateAllSendStatus(tableName, blockHashes);
        }
    }
}
