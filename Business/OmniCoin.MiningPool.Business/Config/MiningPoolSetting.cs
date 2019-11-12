


using OmniCoin.Framework;
using OmniCoin.Tools;
//using OmniCoin.MiningPool.Business.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.MiningPool.Business
{
    public class MiningPoolSetting
    {
        static MiningPoolSetting()
        {
            
            AwardSetting setting = ConfigurationTool.GetAppSettings<AwardSetting>("OmniCoin.MiningPool.Business.conf.json","AwardSetting");

            if (setting != null)
            {
                PoolType = setting.PoolType;
            }
            else
            {
                PoolType = null;
            }
            if(setting == null)
            {
                throw new Exception("config read from OmniCoin.MiningPool.Business.conf.json failed !!!");
            }
            API_URI_MAIN = setting.NodeRpcMainnet;
            API_URI_TEST = setting.NodeRpcTestnet;
            POS_URL_MAIN = setting.PosCheckServiceUrlMainNet;
            POS_URL_TEST = setting.PosCheckServiceUrlTestNet;
        }

        private static string API_URI_MAIN = "";
        private static string API_URI_TEST = "";
        /// <summary>
        /// Node RPC Url
        /// </summary>
        public static string API_URI
        {
            get
            {
                if (GlobalParameters.IsTestnet)
                    return API_URI_TEST;
                else
                    return API_URI_MAIN;
            }
        }

        //private const int POOL_MAIN_PORT = 5009;
        //private const int POOL_TEST_PORT = 5008;
        ///// <summary>
        ///// MiningPool TCP Port
        ///// </summary>
        //public static int POOL_PORT
        //{
        //    get
        //    {
        //        if (GlobalParameters.IsTestnet)
        //            return POOL_TEST_PORT;
        //        else
        //            return POOL_MAIN_PORT;
        //    }
        //}

        //private const string POOL_MAIN_API = @"http://*:5010/";
        //private const string POOL_TEST_API = @"http://*:5011/";
        ///// <summary>
        ///// MiningPool WebAPi Url
        ///// </summary>
        //public static string POOL_API
        //{
        //    get
        //    {
        //        if (GlobalParameters.IsTestnet)
        //            return POOL_TEST_API;
        //        else
        //            return POOL_MAIN_API;
        //    }
        //}

        private static string POS_URL_TEST = "";
        private static string POS_URL_MAIN = "";

        public static string POS_URL
        {
            get
            {
                if (GlobalParameters.IsTestnet)
                    return POS_URL_TEST;
                else
                    return POS_URL_MAIN;
            }
        }

        public static string PoolType
        {
            get;
            set;
        }
    }
}
