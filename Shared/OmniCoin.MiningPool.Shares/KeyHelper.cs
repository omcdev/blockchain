using System;

namespace OmniCoin.MiningPool.Shares
{
    public class KeyHelper
    {
        public static string GetPoolInfoKey(string id)
        {
            var key = string.Format(StringFormats.POOL_INFO, id);
            return key;
            //return $"{id}_MAIN";
        }

        public static string GetPoolWorkingInfoKey(string id)
        {
            var key = string.Format(StringFormats.POOLWORKING_INFO, id);
            return key;
            //return $"{id}_WORKING_MAIN";
        }

        public static string GetMinerEffortKey(string id, int height)
        {
            var key = string.Format(StringFormats.POOL_EFFORT, id, height);
            return key;
            //return $"{id}_MAIN_EFFORT_{height}";
        }

        public static string GetPoolCenterName(bool isTestnet)
        {
            var key = isTestnet ? "POOL-CENTER-TEST" : "POOL-CENTER-MAIN";
            return key;
            //return "MiningPool:POOLCENTER:" + key;
        }

        public static string GetBlockKey(string id)
        {
            return id;
            //return "MiningPool:POOLCENTER:BLOCKINFO:" + id;
        }

    }
}