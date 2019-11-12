


using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Pool.Redis
{
    public class Setting
    {
        public static string[] RedisConnections
        {
            get
            {
                return GlobalParameters.IsTestnet ? RedisConnections_Test : RedisConnections_Main;
            }
        }


        ////127.0.0.1:6371,password=123,defaultDatabase=11,poolsize=10,ssl=false,writeBuffer=10240,prefix=key前辍

        private static string[] RedisConnections_Test = new string[]
        {
            //$"192.168.31.25:6379,{""},defaultDatabase=6,abortConnect=False"
        };

        private static string[] RedisConnections_Main = new string[]
        {
            //"r-3ns10cea37457624.redis.rds.aliyuncs.com:6379,abo,abortConnect=False"
            //$"192.168.31.25:6379,{""},defaultDatabase=6,abortConnect=False"
        };

        public static void Init(string testConnStr,string mainConnStr)
        {
            RedisConnections_Test = new string[] { testConnStr };
            RedisConnections_Main = new string[] { mainConnStr };
        }
    }
}