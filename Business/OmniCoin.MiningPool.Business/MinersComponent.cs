using System;
using System.Collections.Generic;
using OmniCoin.MiningPool.Data;
using OmniCoin.MiningPool.Entities;
using OmniCoin.Consensus;
using OmniCoin.Consensus.Api;
using System.Threading;

namespace OmniCoin.MiningPool.Business
{
    public class MinersComponent
    {
        private static SpinLock spin = new SpinLock();
        /// <summary>
        /// 矿工注册
        /// </summary>
        /// <param name="address"></param>
        /// <param name="account"></param>
        /// <param name="sn"></param>
        /// <returns></returns>
        public Miners RegisterMiner(string address, string account, string sn)
        {
            MinersDac dac = new MinersDac();
            Miners miner = new Miners();
            //验证address是否合法
            //try
            //{
            //    bool isValid = AccountIdHelper.AddressVerify(address);
            //    if (!isValid)
            //    {
            //        throw new ApiCustomException(MiningPoolErrorCode.Miners.ADDRESS_IS_INVALID, "address is invalid");
            //    }
            //}
            //catch
            //{
            //    throw new ApiCustomException(MiningPoolErrorCode.Miners.ADDRESS_IS_INVALID, "address is invalid");
            //}
            //先判断SN和Account的合法性
            string url = MiningPoolSetting.POS_URL + "Api/Account/CheckAccount";
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("MerchantId", account);
            dic.Add("SN", sn);

            string response = ApiHelper.PostApi(url, dic);
            Dictionary<string, string> returnDic = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            if (string.IsNullOrEmpty(returnDic["Data"]))
            {
                throw new ApiCustomException(Int32.Parse(returnDic["Code"]), returnDic["Message"]);
            }

            /* 四种情况
             * 1、地址、SN、Account都不存在直接插入
             * 2、地址不存在，SN、Account存在，修改存在数据状态，插入数据
             * 3、地址存在，SN、Account不存在,抛出地址被占用
             * 4、地址存在，SN、Account存在，Status为1，将Status为0的设置为1，将Status为1的设置为0
             * 5、地址存在、SN、Account存在，Status为0， 直接返回这条记录
             * 6、地址存在，SN和Account不匹配，会更新
             */
            //自旋锁 SpinLock
            bool lockTaken = false;
            try
            {
                //申请获取锁
                spin.Enter(ref lockTaken);
                if (dac.IsAddressExisted(address.Trim()))
                {
                    //地址存在，
                    Miners existMiner = dac.GetMinerByAddress(address.Trim());
                    //先判断地址的状态
                    if (existMiner.Status == 0)
                    {
                        //判断绑定的SN与传入的是否匹配
                        if (existMiner.SN == sn)
                        {
                            dac.UpdateStatus(existMiner.Id, 0, account, sn, Framework.Time.EpochTime);
                            return dac.SelectById(existMiner.Id);
                        }
                        else
                        {
                            //判断旧的的是否匹配
                            Dictionary<string, string> oldDic = new Dictionary<string, string>();
                            oldDic.Add("MerchantId", existMiner.Account);
                            oldDic.Add("SN", existMiner.SN);

                            string oldResponse = ApiHelper.PostApi(url, oldDic);
                            Dictionary<string, string> returnOldDic = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(oldResponse);
                            if (string.IsNullOrEmpty(returnOldDic["Data"]))
                            {
                                //更新Miners表
                                dac.UpdateStatus(existMiner.Id, 0, account, sn, Framework.Time.EpochTime);
                                return dac.SelectById(existMiner.Id);
                            }
                            else
                            {
                                throw new ApiCustomException(MiningPoolErrorCode.Miners.SN_IS_NOT_MATCH_BIND_ADDRESS_SN, $"{existMiner.SN}");
                            }
                        }
                    }
                    else
                    {
                        dac.UpdateStatus(existMiner.Id, 0, account, sn, Framework.Time.EpochTime);
                        return dac.SelectById(existMiner.Id);
                    }
                }
                else
                {
                    //地址不存在
                    //判断数据库中是否存在SN，如果存在SN，先修改address和SN的状态
                    if (returnDic["Data"] == "true")
                    {
                        if (dac.IsSNExisted(sn))
                        {
                            dac.UpdateStatus(1, sn);
                        }
                    }
                    else
                    {
                        throw new ApiCustomException(int.Parse(returnDic["Code"]), returnDic["Message"]);
                    }
                    miner.Address = address;
                    miner.LastLoginTime = Framework.Time.EpochTime;
                    miner.Account = account;
                    miner.SN = sn;
                    miner.Status = 0;
                    miner.Timstamp = Framework.Time.EpochTime;
                    miner.Type = 0;

                    dac.Insert(miner);
                }
            }
            //catch(Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());
            //}
            finally
            {
                //工作完毕，或者发生异常时，检测一下当前线程是否占有锁，如果咱有了锁释放它
                //以避免出现死锁的情况
                if (lockTaken)
                {
                    spin.Exit();
                }
            }
            return miner;
        }

        /// <summary>
        /// 解除SN和Address绑定
        /// </summary>
        /// <param name="address"></param>
        /// <param name="account"></param>
        /// <param name="sn"></param>
        public void PosSNUnbind(string address, string account, string sn)
        {
            MinersDac dac = new MinersDac();
            Miners miner = new Miners();
            //验证address是否合法
            //try
            //{
            //    bool isValid = AccountIdHelper.AddressVerify(address);
            //    if (!isValid)
            //    {
            //        throw new ApiCustomException(MiningPoolErrorCode.Miners.ADDRESS_IS_INVALID, "address is invalid");
            //    }
            //}
            //catch
            //{
            //    throw new ApiCustomException(MiningPoolErrorCode.Miners.ADDRESS_IS_INVALID, "address is invalid");
            //}
            //先判断SN和Account的合法性
            string url = MiningPoolSetting.POS_URL + "Api/Account/CheckAccount";
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("MerchantId", account);
            dic.Add("SN", sn);

            string response = ApiHelper.PostApi(url, dic);
            Dictionary<string, string> returnDic = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            if (string.IsNullOrEmpty(returnDic["Data"]))
            {
                throw new ApiCustomException(int.Parse(returnDic["Code"]), returnDic["Message"]);
            }
            //判断数据库中address是否存在
            if (dac.IsAddressExisted(address.Trim()))
            {
                Miners existMiner = dac.GetMinerByAddress(address.Trim());
                if(existMiner.SN == sn)
                {
                    //解除绑定
                    dac.UpdateStatus(existMiner.Id, 1, account);
                }
                else
                {
                    throw new ApiCustomException(MiningPoolErrorCode.Miners.ACCOUNT_AND_SN_IS_NOT_MATCH, "address and sn is not match");
                }
            }
            else
            {
                throw new ApiCustomException(MiningPoolErrorCode.Miners.ADDRESS_NOT_EXIST, "address not exist");
            }
        }

        public bool MinerLogin(string address, string sn)
        {
            /* 设计思路
             * 1、根据address从数据库获取account，如果数据库没有记录直接抛错误
             * 2、根据account和sn调接口，根据接口返回值提供返回值
             * 
             */
            MinersDac dac = new MinersDac();
            Miners miner = dac.GetMinerByAddress(address);
            if (miner == null)
            {
                throw new ApiCustomException(MiningPoolErrorCode.Miners.ADDRESS_NOT_EXIST, "address not exist");
            }
            string url = MiningPoolSetting.POS_URL + "Api/Account/CheckAccount";
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("MerchantId", miner.Account);
            dic.Add("SN", sn);

            string response = ApiHelper.PostApi(url, dic);
            Dictionary<string, string> returnDic = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            return returnDic["Data"] == "true" ? true : false;
        }

        public bool MiningAuthorize(string address, int type, string sn)
        {
            return false;
        }

        public List<Miners> GetAllMiners()
        {
            MinersDac dac = new MinersDac();
            List<Miners> list = dac.SelectAll();
            return list;
        }

        public Miners GetMinerByAddress(string address)
        {
            MinersDac dac = new MinersDac();
            Miners miner = dac.GetMinerByAddress(address);
            return miner;
        }

        public List<Miners> GetMinersBySN(string sn)
        {
            MinersDac dac = new MinersDac();
            return dac.GetMinersBySN(sn);
        }

        public Miners GetMinerById(long id)
        {
            MinersDac dac = new MinersDac();
            return dac.SelectById(id);
        }

        public void DeleteMiner(long id)
        {
            MinersDac dac = new MinersDac();
            dac.Delete(id);
        }

        public void DeleteMiner(string address)
        {
            MinersDac dac = new MinersDac();
            dac.Delete(address);
        }

        public void UpdateStatus(string sn)
        {
            MinersDac dac = new MinersDac();
            dac.UpdateStatus(1, sn);
        }

        public long GetUnPaidReward(string address)
        {
            MinersDac dac = new MinersDac();
            return dac.GetUnPaidReward(address);
        }

        public long GetPaidReward(string address)
        {
            MinersDac dac = new MinersDac();
            return dac.GetPaidReward(address);
        }

        public void UpdateSendReward(string address, long amount)
        {
            MinersDac dac = new MinersDac();
            dac.UpdateSendReward(address, amount);
        }
    }
}
