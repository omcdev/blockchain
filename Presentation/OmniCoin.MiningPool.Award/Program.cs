using EdjCase.JsonRpc.Client;
using EdjCase.JsonRpc.Core;
using OmniCoin.AliMQ;
using OmniCoin.Consensus.Api;
using OmniCoin.DTO;
using OmniCoin.DTO.Transaction;
using OmniCoin.DTO.Utxo;
using OmniCoin.Framework;
using OmniCoin.MiningPool.Business;
using OmniCoin.MiningPool.Entities;
using OmniCoin.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace OmniCoin.MiningPool.Award
{
    class Program
    {
        static async Task Main(string[] args) 
        {
            try
            {
                //启动参数模板 -testnet L0@dRunn3r@2018 1900292902
                //-testnet  pwd 1568908800000
                bool testnet = false;
                string password = string.Empty;
                if (args.Length > 0 && args[0].ToLower() == "-testnet")
                {
                    testnet = true;
                    GlobalParameters.IsTestnet = true;
                    LogHelper.Info("this is Testnet");
                }
                //加密情况下，主链一个参数，测试链二个参数
                if (testnet)
                {                    
                    //-testnet xxxxxx timestamp
                    if (args.Length == 2 || args.Length == 3)
                    {
                        password = args[1];
                    }
                    else
                    {
                        throw new Exception("testnet parameter is invalid");
                    }
                }
                else
                {
                    //xxxxxx timestamp
                    if (args.Length == 1 || args.Length == 2)
                    {
                        password = args[0];
                    }
                    else
                    {
                        throw new Exception("mainnet parameter is invalid");
                    }
                }
                BlocksComponent blocksComponent = new BlocksComponent();
                RewardListComponent component = new RewardListComponent();
                
                AwardConfig awardConfig = ConfigurationTool.GetAppSettings<AwardConfig>("OmniCoin.MiningPool.Award.conf.json", "MiningPoolAwardSetting");

                RabbitMQ.RabbitMqSetting.CONNECTIONSTRING = awardConfig.RabbitMqConnectString;
                //OmniCoin.Pool.Redis.Setting.Init(awardConfig.RedisTestnetConnections, awardConfig.RedisMainnetConnections);
                MiningPool.Data.DataAccessComponent.MainnetConnectionString = awardConfig.MySqlMainnetConnectString;
                MiningPool.Data.DataAccessComponent.TestnetConnectionString = awardConfig.MySqlTestnetConnectString;
                string nodeRpcUrl = testnet ? awardConfig.NodeRpcTestnet : awardConfig.NodeRpcMainnet;
                try
                {
                    LogHelper.Info("*************************begin to Prepare*******************************");
                    //先获取所有没有发放的区块数据
                    //long? timestamp = 1550156998740; //null;
                    long? timestamp = null;
                    if (testnet)
                    {
                        if (args.Length == 3)
                        {
                            timestamp = Convert.ToInt64(args[2]);
                        }
                    }
                    else
                    {
                        if (args.Length == 2)
                        {
                            timestamp = Convert.ToInt64(args[1]);
                        }
                    }
                    List<Blocks> blocks = blocksComponent.GetAllUnRewardBlocks(timestamp);
                    LogHelper.Info($"Get blocks count is {blocks.Count}, Get blocks latest timestamp is {blocks.OrderByDescending(q=>q.Timstamp).First().Timstamp}");
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    HashSet<string> rewardTables = new HashSet<string>();
                    //合并相同的RewardTable
                    foreach (var b in blocks)
                    {
                        string tableName = "RewardList" + Time.GetLocalDateTime(b.Timstamp).ToString("yyyyMMdd");
                        rewardTables.Add(tableName);
                        rewardTables.Add(tableName);
                        if (b.IsDiscarded == 0)
                        {
                            sb.Append($"'{b.Hash}'");
                            sb.Append(",");
                        }
                    }
                    string blockHashes = sb.ToString().TrimEnd(',');
                    //遍历不同的RewardTable
                    List<RewardList> rewardList = new List<RewardList>();
                    foreach (string tableName in rewardTables)
                    {
                        List<RewardList> tempRewardList = component.GetAllUnPaidRewardGroup(tableName, blockHashes);                        
                        foreach(var temp in tempRewardList)
                        {
                            rewardList.Add(temp);
                        }
                    }
                    LogHelper.Info($"####################bgein to send ####################");
                    //取出表中所有没有发放奖励的
                    if (rewardList != null && rewardList.Count > 0)
                    {
                        List<SendReward> sendRewards = new List<SendReward>();
                        List<SendManyOutputIM> many = new List<SendManyOutputIM>();
                        //合并相同的地址
                        var result = rewardList.GroupBy(p => p.MinerAddress).Select(p => new SendReward { Address = p.Key, OriginalReward = p.Sum(a => a.OriginalReward), Amount = p.Sum(a => a.ActualReward) });
                        LogHelper.Info($"merge result count {result.Count()}");  
                        //long totalExtractAward = result.Sum(p => p.OriginalReward);
                        //long totalActualReward = result.Sum(p => p.ActualReward);
                        //组装Receiver
                        //List<SendRawTransactionOutputsIM> receiversList = new List<SendRawTransactionOutputsIM>();
                        //receiversList.Add(new SendRawTransactionOutputsIM { Address = setting.ExtractReceiveAddress, Amount = Convert.ToInt64(Math.Floor(totalExtractAward * setting.ServiceFeeProportion)) });
                        foreach (var item in result)
                        {
                            if (item.Amount != 0)
                            {
                                //receiversList.Add(new SendRawTransactionOutputsIM { Address = item.Address, Amount = item.ActualReward });
                                sendRewards.Add(new SendReward { Address = item.Address, Amount = item.Amount, OriginalReward = item.OriginalReward });
                            }
                            else
                            {
                                //直接更新数据库
                                foreach (var point in rewardTables)
                                {
                                    component.UpdateNullPaidStatus(point, item.Address, blockHashes);
                                }
                            }
                        }
                        //每次发送固定数目的
                        int count = awardConfig.SendCount;
                        List<List<SendReward>> listGroup = new List<List<SendReward>>();
                        //分页获取固定数目的奖励
                        for (int i = 0; i < sendRewards.Count; i += count)
                        {
                            List<SendReward> cList = new List<SendReward>();
                            cList = sendRewards.Skip(i).Take(count).ToList();
                            listGroup.Add(cList);
                        }
                        //先获取一次ListPageUnspent
                        int index = 0;
                        int currentPage = 1;
                        int pageSize = 1000;
                        int tryTimes = 0;
                        AuthenticationHeaderValue authHeaderValue = null;
                        RpcClient client = new RpcClient(new Uri(nodeRpcUrl), authHeaderValue, null, null, "application/json");
                        //不够的话继续获取
                        ListPageUnspentOM unspent = await GetUnspentOM(nodeRpcUrl,currentPage, pageSize, testnet, tryTimes);
                        if(unspent == null)
                        {
                            return;
                        }
                        tryTimes = 0;
                        //分页发放奖励
                        foreach (List<SendReward> item in listGroup)
                        {
LabelSendItem:
                            //组装Sender   加上9%的手续费发放
                            long totalItemOriginAmount = item.Sum(p => p.OriginalReward);
                            long totalItemActualAmount = item.Sum(p => p.Amount);
                            long totalAmount = 0;
                            //类型转为SendRawTransactionOutputsIM
                            List<SendRawTransactionOutputsIM> receiverList = item.ToList<SendRawTransactionOutputsIM>();
                            //添加服务费交易
                            //receiverList.Add(new SendRawTransactionOutputsIM { Address = awardConfig.ExtractReceiveAddress, Amount = Convert.ToInt64(Math.Floor(totalItemOriginAmount * awardConfig.ServiceFeeProportion)) });
                            long totaloutputAmount = totalItemActualAmount;// + Convert.ToInt64(Math.Floor(totalItemOriginAmount * awardConfig.ServiceFeeProportion));
                            List<SendRawTransactionInputsIM> sendersList = new List<SendRawTransactionInputsIM>();
                            //先遍历获取可用的utxo

                            while (index < unspent.UnspentOMList.Count)
                            {
                                if (unspent.UnspentOMList[index].spendable == false)
                                {
                                    if (sendersList.Count > 0 && sendersList.Any(q => q.TxId == unspent.UnspentOMList[index].txid && q.Vout == unspent.UnspentOMList[index].vout))
                                    {
                                        //组合中存在相同的数据了
                                        LogHelper.Info($"already exists avaliable txhash is {unspent.UnspentOMList[index].txid}, avaliable vout is {unspent.UnspentOMList[index].vout} and index is {index}");
                                    }
                                    else
                                    {
                                        LogHelper.Info($"avaliable txhash is {unspent.UnspentOMList[index].txid}, avaliable vout is {unspent.UnspentOMList[index].vout} and index is {index}, totalAmount is {totalAmount}, totaloutputAmount is {totaloutputAmount}");
                                        sendersList.Add(new SendRawTransactionInputsIM { TxId = unspent.UnspentOMList[index].txid, Vout = Convert.ToInt32(unspent.UnspentOMList[index].vout) });
                                        totalAmount += unspent.UnspentOMList[index].amount;
                                    }
                                }
                                else
                                {
                                    //已经被花费了
                                    LogHelper.Info($"unavaliable txhash is {unspent.UnspentOMList[index].txid}, avaliable vout is {unspent.UnspentOMList[index].vout} and index is {index}");
                                }
                                index++;
                                if (index >= unspent.UnspentOMList.Count)
                                {
                                    currentPage++;
                                    unspent = await GetUnspentOM(nodeRpcUrl, currentPage, pageSize, testnet, tryTimes);
                                    if(unspent == null)
                                    {
                                        return;
                                    }
                                    index = 0;
                                    tryTimes = 0;
                                }
                                if(totalAmount > totaloutputAmount + 100000000)
                                {
                                    break;
                                }
                            }
                            foreach (var receiver in receiverList)
                            {
                                LogHelper.Info($"send reward to {receiver.Address}, send amount is {receiver.Amount}");
                            }
                            //调用接口SendRawTransaction，发送交易
                            string txHash = "";
                            try
                            {
                                //先解密
                                AuthenticationHeaderValue auth = null;
                                RpcClient rpcClient = new RpcClient(new Uri(testnet ? awardConfig.NodeRpcTestnet : awardConfig.NodeRpcMainnet), auth, null, null, "application/json");
                                RpcRequest passphraseRequest = RpcRequest.WithParameterList("WalletPassphrase", new[] { password }, 1);
                                RpcResponse passphraseResponse = await rpcClient.SendRequestAsync(passphraseRequest);
                                if (passphraseResponse.HasError)
                                {
                                    LogHelper.Error(passphraseResponse.Error.Message.ToString());
                                    throw new Exception(passphraseResponse.Error.Message.ToString());
                                }
                                if (!passphraseResponse.GetResult<bool>())
                                {
                                    LogHelper.Error(passphraseResponse.Error.Message.ToString());
                                    throw new Exception(passphraseResponse.Error.Message.ToString());
                                }

                                RpcRequest request = RpcRequest.WithParameterList("SendRawTransaction", new List<object> { sendersList.ToArray(), receiverList.ToArray(), awardConfig.ChangeAddress, 0, awardConfig.FeeRate }, 1);
                                RpcResponse response = await client.SendRequestAsync(request);
                                if (response.HasError)
                                {
                                    /*
                                    if(response.Error.Message.Contains("2020005"))
                                    {
                                        RpcRequest passphraseRequest = RpcRequest.WithParameterList("WalletPassphrase", new[] { password }, 1);
                                        RpcResponse passphraseResponse = await rpcClient.SendRequestAsync(passphraseRequest);
                                        response = await client.SendRequestAsync(request);
                                    }
                                    */
                                    LogHelper.Error(response.Error.Message.ToString());
                                    throw new Exception(response.Error.Message.ToString());
                                }
                                txHash = response.GetResult<string>();
                            }
                            catch(Exception exc)
                            {
                                LogHelper.Error(exc.Message, exc);
                                if(exc.Message.Contains("1010001"))
                                {
                                    LogHelper.Warn("start to resend, reason 1010001");
                                    goto LabelSendItem;
                                }
                                
                                if(exc.Message.Contains("2020002"))
                                {
                                    LogHelper.Warn("start to resend, reason 2020002");
                                    unspent = await GetUnspentOM(nodeRpcUrl, currentPage, pageSize, testnet, 0);
                                    if(unspent == null)
                                    {
                                        return;
                                    }
                                    index = 0;
                                    tryTimes = 0;
                                    goto LabelSendItem;
                                }
                                if(sendersList.Count <= 0)
                                {
                                    return;
                                }
                                System.Threading.Thread.Sleep(90000);

                                RpcRequest request = RpcRequest.WithParameterList("GetTxHashByInput", new List<object> { sendersList.First().TxId, sendersList.First().Vout }, 1);
                                RpcResponse response = await client.SendRequestAsync(request);
                                if(response.HasError)
                                {
                                    LogHelper.Error(response.Error.Message.ToString());
                                    throw new Exception(response.Error.Message.ToString());
                                }
                                txHash = response.GetResult<string>();
                                if(string.IsNullOrEmpty(txHash))
                                {
                                    return;
                                }
                            }
                            //记录一下发放日志：地址，金额，期数，锁定时间，交易Hash
                            LogHelper.Info($"this transaction hash is: {txHash}");
                            //消息队列
                            //LogHelper.Info($"begin to send ali mesage queue");
                            LogHelper.Info($"begin to send rabbit mesage queue");
                            //先写日志
                            MinersComponent minersComponent = new MinersComponent();
                            List<AliMQ.RewardSendMQ> mqList = new List<AliMQ.RewardSendMQ>();
                            //发放消息队列
                            foreach (var receiver in item)
                            {
                                //if (receiver.Address != awardConfig.ExtractReceiveAddress)
                                //{
                                    //LogHelper.Info($"send reward to {receiver.Address}, send amount is {receiver.Amount}");
                                    //根据address获取矿工信息
                                    Miners entity = minersComponent.GetMinerByAddress(receiver.Address);
                                    if (entity != null)
                                    {
                                        mqList.Add(new AliMQ.RewardSendMQ { Address = receiver.Address, SN = entity.SN, Account = entity.Account, Reward = receiver.Amount, CurrentDate = Time.EpochTime });
                                    }
                                //}
                            }
                            //发送阿里云消息队列
                            //producer.SendNormalMessage(mqList, txHash);
                            //发送Rabbit MQ消息队列
                            RabbitMQ.RabbitMqClient.ProduceMessage<RewardSendMQ>(mqList);

                            //更新数据库状态
                            string addresses = string.Empty;
                            item.ForEach(q =>
                            {
                                addresses += $"'{q.Address}',";
                            });
                            addresses = addresses.TrimEnd(',');
                            LogHelper.Info($"begin to update reward list txHash is {txHash}");
                            foreach (var t in rewardTables)
                            {
                                LogHelper.Info($"begin to update reward table {t}");
                                component.UpdatePaidStatusByAddresses(t, addresses, txHash, blockHashes);
                                System.Threading.Thread.Sleep(awardConfig.UpdateRewardSleepTime);
                            }
                            //更新miners表UnpaidRewrad和PaidRewrad状态
                            foreach(SendRawTransactionOutputsIM point in item)
                            {
                                //更新miners表UnpaidRewrad和PaidRewrad状态
                                LogHelper.Info($"begin to update Miners the address is {point.Address}, amount is {point.Amount}");
                                minersComponent.UpdateSendReward(point.Address, point.Amount);
                            }
                            /*
                            foreach (SendRawTransactionOutputsIM point in item)
                            {
                                LogHelper.Info($"begin to update reward list the address is {point.Address}, txHash is {txHash}");
                                foreach (var t in rewardTables)
                                {
                                    //这个有问题
                                    LogHelper.Info($"begin to update reward table {t}");
                                    component.UpdatePaidStatus(t, point.Address, txHash, blockHashes);
                                }
                                //更新miners表UnpaidRewrad和PaidRewrad状态
                                minersComponent.UpdateSendReward(point.Address, point.Amount);
                            }
                            */
                            System.Threading.Thread.Sleep(awardConfig.CircleSleepTime);
                        }
                        //更新Blocks表中发送状态。
                        foreach (var t in rewardTables)
                        {
                            blocksComponent.UpdateAllSendStatus(t, blockHashes);
                        }
                        LogHelper.Info("*************************end to send this cricle*******************************");
                    }
                    
                    LogHelper.Info("*************************end to send*******************************");
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex.Message, ex);
                }
                /*
                finally
                {
                    producer.SendDispose();
                }
                */
            }
            catch (Exception exc)
            {
                LogHelper.Error(exc.Message, exc);
            }
        }

        static async Task<ListPageUnspentOM> GetUnspentOM(string nodeRpcUrl,int currentPage, int pageSize, bool isTestNet, int tryTimes)
        {
            ListPageUnspentOM unspent = null;
            try
            {
                AuthenticationHeaderValue authHeaderValue = null;
                RpcClient client = new RpcClient(new Uri(nodeRpcUrl), authHeaderValue, null, null, "application/json");
                RpcRequest request = RpcRequest.WithParameterList("ListPageUnspentNew", new List<object> { currentPage, pageSize }, 1);
                RpcResponse response = await client.SendRequestAsync(request);
                if (response.HasError)
                {
                    LogHelper.Error(response.Error.Message.ToString());
                }
                unspent = response.GetResult<ListPageUnspentOM>();
            }
            catch(Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
                tryTimes++;

                if (tryTimes >= 3)
                {
                    return null;
                }
                else
                {
                    System.Threading.Thread.Sleep(3000);
                    unspent = await GetUnspentOM(nodeRpcUrl, currentPage, pageSize, isTestNet, tryTimes);
                }
            }
            return unspent;
        }

        #region 注释

        //static async Task<List<SendRawTransactionInputsIM>> GetEnougtTxHash(long totaloutputAmount, bool testnet)
        //{
        //    long totalAmount = 0;
        //    List<SendRawTransactionInputsIM> sendersList = new List<SendRawTransactionInputsIM>();
        //    int i = 1;
        //    try
        //    {
        //        while (totalAmount < totaloutputAmount)
        //        {
        //            AuthenticationHeaderValue authHeaderValue = null;
        //            RpcClient client = new RpcClient(new Uri(testnet ? "http://127.0.0.1:5006/" : "http://127.0.0.1:5007/"), authHeaderValue, null, null, "application/json");
        //            RpcRequest transactionRequest = RpcRequest.WithParameterList("ListPageUnspent", new List<object> { 1, i, 300, 9999999, 1, 9999999999999999, true }, 1);
        //            RpcResponse transactionResponse = await client.SendRequestAsync(transactionRequest);
        //            if (transactionResponse.HasError)
        //            {
        //                LogHelper.Error(transactionResponse.Error.Message.ToString());
        //                throw new Exception(transactionResponse.Error.Message.ToString());
        //            }
        //            ListPageUnspentOM unspent = transactionResponse.GetResult<ListPageUnspentOM>();

        //            foreach (var senderItem in unspent.UnspentOMList)
        //            {
        //                //判断总的输出金额和
        //                if (totalAmount < totaloutputAmount)
        //                {
        //                    if (senderItem.spendable == false && senderItem.confirmations > 100)
        //                    {
        //                        LogHelper.Info($"avaliable txhash is {senderItem.txid}, avaliable vout is {senderItem.vout}");
        //                        sendersList.Add(new SendRawTransactionInputsIM { TxId = senderItem.txid, Vout = Convert.ToInt32(senderItem.vout) });
        //                        totalAmount += senderItem.amount;
        //                    }
        //                    else
        //                    {
        //                        LogHelper.Info($"unavaliable txhash is {senderItem.txid}, avaliable vout is {senderItem.vout}");
        //                    }
        //                }
        //                else
        //                {
        //                    LogHelper.Info($"get enough txhash successful");
        //                    break;
        //                }
        //            }
        //            i++;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(ex.Message);
        //    }
        //    return sendersList;
        //}

        #endregion
    }
}
