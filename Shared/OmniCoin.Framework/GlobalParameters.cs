


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Framework
{
    public class GlobalParameters
    {
        static GlobalParameters()
        {
            LocalHeight = -1;
            LocalConfirmedHeight = -1;
        }

        public static long ConfigmedAmount { get; set; }
        public static long UnConfigmedAmount { get; set; }
        public static long TotalAmount { get; set; }

        public static bool IsLoadTransRecord { get; set; }
        public static bool IsTestnet { get; set; }
        public static long LocalHeight { get; set; }
        public static long LatestBlockTime { get; set; }
        public static long LocalConfirmedHeight { get; set; }

        private const String CACHE_FILE_TEST = "Temp/cache_test";
        private const String CACHE_FILE_MAIN = "Temp/cache";

        public const String CACHE_FILE_DIR = "Temp";

        public static String CACHE_FILE
        {
            get
            {
                return IsTestnet ? CACHE_FILE_TEST : CACHE_FILE_MAIN;
            }
        }

        public static bool IsPool { get; set; }

        public static bool IsExplorer { get; set; } = false;
    }
}
