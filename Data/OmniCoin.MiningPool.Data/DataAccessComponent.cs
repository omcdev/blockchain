using OmniCoin.Framework;
using MySql.Data.MySqlClient;

namespace OmniCoin.MiningPool.Data
{
    public abstract class DataAccessComponent
    {
        public static string TestnetConnectionString = "";
        public static string MainnetConnectionString = "";

        public string CacheConnectionString
        {
            get
            {
                return GlobalParameters.IsTestnet ? TestnetConnectionString : MainnetConnectionString;
            }
        }

        protected static T GetDataValue<T>(System.Data.IDataReader dr, string columnName)
        {
            int i = dr.GetOrdinal(columnName);
            var mydr = (System.Data.SqlClient.SqlDataReader)dr;
            if (!dr.IsDBNull(i))
            {
                return mydr.GetFieldValue<T>(i);
            }
            else
            {
                return default(T);
            }
        }

        protected static T GetDataValue<T>(MySqlDataReader dr, string columnName)
        {
            int i = dr.GetOrdinal(columnName);
            var mydr = (MySqlDataReader)dr;
            if (!dr.IsDBNull(i))
            {
                return mydr.GetFieldValue<T>(i);
            }
            else
            {
                return default(T);
            }
        }
    }
}
