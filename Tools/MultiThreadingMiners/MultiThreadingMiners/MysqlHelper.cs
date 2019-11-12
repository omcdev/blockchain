using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace MultiThreadingMiners
{
    public class MysqlHelper
    {        
        public static string CONNECTIONSTRING = "server=192.168.31.25;port=53306;user=MySqlDockerUser;password=1;database=omnicoin_pool_test;SslMode=none;";

        public Dictionary<string, string> GetAllMiners()
        {
            //StringBuilder sb = new StringBuilder();

            //for (int i = 1; i <= 200; i++)
            //{
            //    sb.Append($"'POSTESTMINER{(i).ToString("000")}',");
            //}
            //string minerSNs = sb.ToString().TrimEnd(',');

            //LogHelper.Info($"SELECT Address, SN FROM Miners Where Id <> 46;");
            //string SQL_STATEMENT = $"SELECT SN, Address FROM Miners WHERE Id <> 46;";

            Dictionary<string, string> result = new Dictionary<string, string>();
            LogHelper.Info($"SELECT Address, SN FROM Miners Where Id Id < 15898;");
            string SQL_STATEMENT = $"SELECT SN, Address FROM Miners WHERE Id < 15898;";
            using (MySqlConnection conn = new MySqlConnection(CONNECTIONSTRING))
            {
                using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
                {
                    cmd.Connection.Open();
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            result.Add(dr.GetString(0), dr.GetString(1));
                        }
                    }
                }
            }

            return result;
        }
    }
}
