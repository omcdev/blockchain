


using OmniCoin.Update.Db;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Update
{
    public class DbDomains
    {
        const string TempFileMainnet = "Temp/User";
        const string TempFileTestnet = "Temp_test/User";

        const string UserFileMainnet = "Temp/User";
        const string UserFileTestnet = "Temp_test/User";

        const string SqlFileMainnet = "OmniCoin_test.db";
        const string SqlFileTestnet = "OmniCoin.db";

        public static string TempFile
        {
            get
            {
                return Framework.GlobalParameters.IsTestnet ? TempFileTestnet : TempFileMainnet;
            }
        }

        public static string UserFile
        {
            get
            {
                return Framework.GlobalParameters.IsTestnet ? UserFileTestnet : UserFileMainnet;
            }
        }

        public static string SqliteFile
        {
            get
            {
                return Framework.GlobalParameters.IsTestnet ? SqlFileMainnet : SqlFileTestnet;
            }
        }

        public static LevelDomain UserDomain;

        public static void Init()
        {
            UserDomain = new LevelDomain(UserFile);
        }

        public static void Close()
        {
            UserDomain.Close();
        }
    }
}