using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BlockAnalysisTools
{
    public class BlockAnalysisDb
    {
        public static OmniCoin.Data.LevelDomain DB
        {
            get
            {
                return BlockAnalysisDbBase.BlockAnalysisDb;
            }
        }

        public static string GetKey(string catelog, string key)
        {
            return catelog + ":" + key;
        }
    }

    public class BlockAnalysisDbBase
    {
       public static OmniCoin.Data.LevelDomain BlockAnalysisDb;

        static BlockAnalysisDbBase()
        {
            BlockAnalysisDb =  new OmniCoin.Data.LevelDomain(Path.Combine("LevelDB_BlockAnalysis"));
        }
    }

    public class LevelDBType
    {
        public static string Height_BlockHash = "Height_BlockHash";

        public static string BlockHash_Height = "BlockHash_Height";
        
        public static string Height_BlockData = "Height_BlockData";

        public static string Day_Blocks = "Day_Blocks";

        public static string Day_TxHashs = "Day_TxHashs";

        public static string Day_Blocks_Counts = "Day_Blocks_Counts";

        public static string Day_TxHashs_Counts = "Day_TxHashs_Counts";

        public static string TxHash = "TxHash";
    }
}

