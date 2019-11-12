


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Update.SqliteDb
{
    public static class SqlDb
    {
        public static string ConnectionString
        {
            get
            {
                return OmniCoin.Framework.GlobalParameters.IsTestnet ? ConnectionString_Test : ConnectionString_Main;
            }
        }

        private const string ConnectionString_Test = "Filename=./OmniCoin_test.db; Mode=ReadWriteCreate;Cache=Shared;";
        private const string ConnectionString_Main = "Filename=./OmniCoin.db; Mode=ReadWriteCreate;Cache=Shared;";
    }
}
