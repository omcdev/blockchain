using FiiiChain.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace FiiiChain.KafkaMQ
{
    public class KafkaInfo
    {
        private static string IP_MAIN = "172.31.126.51:9092";
        //47.75.251.9:8337服务器IP地址
        private static string IP_TEST = "172.31.126.43:9092";

        internal static string IP
        {
            get
            {
                return GlobalParameters.IsTestnet ? IP_TEST : IP_MAIN;
            }
        }

        public static string MqName;
    }
}