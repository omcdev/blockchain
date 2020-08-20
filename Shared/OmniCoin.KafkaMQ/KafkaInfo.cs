using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.KafkaMQ
{
    public class KafkaInfo
    {
        private static string IP_MAIN = "172.31.126.51:9092";

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