


using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.MiningPool.Shares
{
    internal class StringFormats
    {
        private const string POOL_INFO_TEST = "{0}_TEST";
        private const string POOL_INFO_MAIN = "{0}_MAIN";

        internal static string POOL_INFO
        {
            get
            {
                return GlobalParameters.IsTestnet ? POOL_INFO_TEST : POOL_INFO_MAIN;
            }
        }

        private const string POOL_EFFORT_TEST = "{0}_TEST_EFFORT_{1}";
        private const string POOL_EFFORT_MAIN = "{0}_MAIN_EFFORT_{1}";

        internal static string POOL_EFFORT
        {
            get
            {
                return GlobalParameters.IsTestnet ? POOL_EFFORT_TEST : POOL_EFFORT_MAIN;
            }
        }

        private const string POOLWORKING_INFO_TEST = "{0}_WORKING_TEST";
        private const string POOLWORKING_INFO_MAIN = "{0}_WORKING_MAIN";

        internal static string POOLWORKING_INFO
        {
            get
            {
                return GlobalParameters.IsTestnet ? POOLWORKING_INFO_TEST : POOLWORKING_INFO_MAIN;
            }
        }
    }
}
