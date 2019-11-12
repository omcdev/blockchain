


using OmniCoin.Framework;
using OmniCoin.MiningPool.Business;
using OmniCoin.MiningPool.Entities;
using System;

namespace OmniCoin.Pool.Apis
{
    /// <summary>
    /// 矿池接口
    /// </summary>
    internal class MinerApi
    {
        /// <summary>
        /// 矿工身份验证
        /// </summary>
        /// <param name="address"></param>
        /// <param name="sn"></param>
        /// <returns></returns>
        public static bool ValidateMiner(string address, string sn)
        {
            try
            {
                MinersComponent component = new MinersComponent();
                var miner = component.GetMinerByAddress(address);
                if (miner == null || miner.SN != sn || miner.Status !=0)
                {
                    bool isValidate = component.MinerLogin(address, sn);
                    return isValidate;
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
                return false;
            }
        }

        /// <summary>
        /// 保存矿工信息
        /// </summary>
        /// <param name="address"></param>
        /// <param name="account"></param>
        /// <param name="sn"></param>
        /// <returns></returns>
        public static Miners SaveMiners(string address, string account, string sn)
        {
            try
            {
                MinersComponent component = new MinersComponent();
                Miners entity = component.RegisterMiner(address, account, sn);
                return entity;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
                return null;
            }
        }

        /// <summary>
        /// 账号登录
        /// </summary>
        /// <param name="address">钱包地址</param>
        /// <param name="type">类型，0是Pos机，1是手机</param>
        /// <param name="sn">SerialNo, Pos是SN，手机是IMEI</param>
        /// <returns></returns>
        public static bool MiningAuthorize(string address, int type, string sn)
        {
            try
            {
                MinersComponent component = new MinersComponent();
                bool result = component.MiningAuthorize(address, type, sn);
                return result;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
                return false;
            }
        }
    }
}
