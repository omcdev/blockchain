using OmniCoin.Framework;
using OmniCoin.MiningPool.Entities;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace OmniCoin.MiningPool.Data
{
    public class MinersDac : DataAccessComponent
    {
        public void Insert(Miners miner)
        {
            const string SQL_STATEMENT =
                "INSERT INTO Miners " +
                "(Address, Account, Type, SN, Status, Timstamp, LastLoginTime, UnpaidReward, PaidReward) " +
                "VALUES (@Address, @Account, @Type, @SN, @Status, @Timstamp, @LastLoginTime, 0, 0);";
            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Parameters.AddWithValue("@Address", miner.Address);
                cmd.Parameters.AddWithValue("@Account", miner.Account);
                cmd.Parameters.AddWithValue("@Type", miner.Type);
                cmd.Parameters.AddWithValue("@SN", miner.SN);
                cmd.Parameters.AddWithValue("@Status", miner.Status);
                cmd.Parameters.AddWithValue("@Timstamp", miner.Timstamp);
                cmd.Parameters.AddWithValue("@LastLoginTime", miner.LastLoginTime);

                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(long id)
        {
            const string SQL_STATEMENT =
                "DELETE FROM Miners " +
                "WHERE Id = @Id;";

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(string address)
        {
            const string SQL_STATEMENT =
                "DELETE FROM Miners " +
                "WHERE Address = @Address;";

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Parameters.AddWithValue("@Address", address);
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        public List<Miners> SelectAll()
        {
            const string SQL_STATEMENT =
                "SELECT Id, Address, Account, Type, SN, Status, Timstamp, LastLoginTime, UnpaidReward, PaidReward " +
                "FROM Miners WHERE Status = 0;";

            List<Miners> result = null;

            //LogHelper.Info("CacheConnectionString" + CacheConnectionString);
            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    result = new List<Miners>();

                    while (dr.Read())
                    {
                        Miners miner = new Miners();
                        miner.Id = GetDataValue<long>(dr, "Id");
                        miner.Address = GetDataValue<string>(dr, "Address");
                        miner.Account = GetDataValue<string>(dr, "Account");
                        miner.Type = GetDataValue<int>(dr, "Type");
                        miner.SN = GetDataValue<string>(dr, "SN");
                        miner.Status = GetDataValue<int>(dr, "Status");
                        miner.Timstamp = GetDataValue<long>(dr, "Timstamp");
                        miner.LastLoginTime = GetDataValue<long>(dr, "LastLoginTime");
                        miner.UnpaidReward = GetDataValue<long>(dr, "UnpaidReward");
                        miner.PaidReward = GetDataValue<long>(dr, "PaidReward");

                        result.Add(miner);
                    }
                }
            }

            return result;
        }

        public Miners SelectById(long id)
        {
            const string SQL_STATEMENT =
                "SELECT Id, Address, Account, Type, SN, Status, Timstamp, LastLoginTime, UnpaidReward, PaidReward " +
                "FROM Miners WHERE Id=@Id;";

            Miners miner = null;

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        miner = new Miners();
                        miner.Id = GetDataValue<long>(dr, "Id");
                        miner.Address = GetDataValue<string>(dr, "Address");
                        miner.Account = GetDataValue<string>(dr, "Account");
                        miner.Type = GetDataValue<int>(dr, "Type");
                        miner.SN = GetDataValue<string>(dr, "SN");
                        miner.Status = GetDataValue<int>(dr, "Status");
                        miner.Timstamp = GetDataValue<long>(dr, "Timstamp");
                        miner.LastLoginTime = GetDataValue<long>(dr, "LastLoginTime");
                        miner.UnpaidReward = GetDataValue<long>(dr, "UnpaidReward");
                        miner.PaidReward = GetDataValue<long>(dr, "PaidReward");
                    }
                }
            }

            return miner;
        }
        
        public Miners GetMinerByAddress(string address)
        {
            const string SQL_STATEMENT =
                "SELECT Id, Address, Account, Type, SN, Status, Timstamp, LastLoginTime, UnpaidReward, PaidReward " +
                "FROM Miners WHERE Address=@Address LIMIT 1;";

            Miners miner = null;

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
                {
                    cmd.Parameters.AddWithValue("@Address", address);
                    cmd.Connection.Open();
                    cmd.CommandTimeout = 1200;
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            miner = new Miners();
                            miner.Id = GetDataValue<long>(dr, "Id");
                            miner.Address = GetDataValue<string>(dr, "Address");
                            miner.Account = GetDataValue<string>(dr, "Account");
                            miner.Type = GetDataValue<int>(dr, "Type");
                            miner.SN = GetDataValue<string>(dr, "SN");
                            miner.Status = GetDataValue<int>(dr, "Status");
                            miner.Timstamp = GetDataValue<long>(dr, "Timstamp");
                            miner.LastLoginTime = GetDataValue<long>(dr, "LastLoginTime");
                            miner.UnpaidReward = GetDataValue<long>(dr, "UnpaidReward");
                            miner.PaidReward = GetDataValue<long>(dr, "PaidReward");
                        }
                    }
                }
            }

            return miner;
        }

        public List<Miners> GetMinersBySN(string sn)
        {
            const string SQL_STATEMENT =
                "SELECT Id, Address, Account, Type, SN, Status, Timstamp, LastLoginTime, UnpaidReward, PaidReward " +
                "FROM Miners WHERE SN=@SN AND Status = 0;";

            List<Miners> miners = null;

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Parameters.AddWithValue("@SN", sn);
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Miners miner = new Miners();
                        miner.Id = GetDataValue<long>(dr, "Id");
                        miner.Address = GetDataValue<string>(dr, "Address");
                        miner.Account = GetDataValue<string>(dr, "Account");
                        miner.Type = GetDataValue<int>(dr, "Type");
                        miner.SN = GetDataValue<string>(dr, "SN");
                        miner.Status = GetDataValue<int>(dr, "Status");
                        miner.Timstamp = GetDataValue<long>(dr, "Timstamp");
                        miner.LastLoginTime = GetDataValue<long>(dr, "LastLoginTime");
                        miner.UnpaidReward = GetDataValue<long>(dr, "UnpaidReward");
                        miner.PaidReward = GetDataValue<long>(dr, "PaidReward");

                        miners.Add(miner);
                    }
                }
            }

            return miners;
        }

        public void UpdateAccount(long id, string account)
        {
            const string SQL_STATEMENT =
                "UPDATE Miners " +
                "SET Account = @Account " +
                "WHERE Id = @Id;";

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Account", account);

                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateStatus(long id, int status, string account)
        {
            const string SQL_STATEMENT =
                "UPDATE Miners " +
                "SET Status = @Status, Account = @Account, LastLoginTime = @LastLoginTime " +
                "WHERE Id = @Id;";

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@Account", account);
                cmd.Parameters.AddWithValue("@LastLoginTime", Time.EpochTime);

                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateStatus(long id, int status, string account, string sn, long timestamp)
        {
            const string SQL_STATEMENT = "Update Miners Set Status = 1 Where SN = @SN;UPDATE Miners SET Status = @Status, Account = @Account, SN = @SN, Timstamp = @Timstamp WHERE Id = @Id;";

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@Account", account);
                cmd.Parameters.AddWithValue("@SN", sn);
                cmd.Parameters.AddWithValue("@Timstamp", timestamp);

                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateStatus(int status, string sn)
        {
            const string SQL_STATEMENT =
                "UPDATE Miners " +
                "SET Status = @Status, Timstamp = @Timstamp " +
                "WHERE SN = @SN AND Status = 0;";

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Parameters.AddWithValue("@Timstamp", Framework.Time.EpochTime);
                cmd.Parameters.AddWithValue("@SN", sn);
                cmd.Parameters.AddWithValue("@Status", status);

                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        public bool IsExisted(long id)
        {
            const string SQL_STATEMENT =
                "SELECT 1 " +
                "FROM Miners " +
                "WHERE Id = @Id;";

            bool hasMiner = false;

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Parameters.AddWithValue("@Id", id);

                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    hasMiner = dr.HasRows;
                }
            }

            return hasMiner;
        }

        public bool IsSNExisted(string sn)
        {
            const string SQL_STATEMENT =
                "SELECT 1 " +
                "FROM Miners " +
                "WHERE SN = @SN;";

            bool hasSN = false;

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Parameters.AddWithValue("@SN", sn);

                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    hasSN = dr.HasRows;
                }
            }

            return hasSN;
        }

        public bool IsAddressExisted(string address)
        {
            const string SQL_STATEMENT =
                "SELECT 1 " +
                "FROM Miners " +
                "WHERE Address = @Address;";

            bool hasAddress = false;
            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Parameters.AddWithValue("@Address", address);

                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    hasAddress = dr.HasRows;
                }
            }

            return hasAddress;
        }

        public Miners GetActivityMiner(string account, string sn)
        {
            const string SQL_STATEMENT =
                "SELECT Id, Address, Account, Type, SN, Status, Timstamp, LastLoginTime, UnpaidReward, PaidReward " +
                "FROM Miners WHERE Account=@Account AND SN=@SN AND Status=0 LIMIT 1;";

            Miners miner = null;

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
                {
                    cmd.Parameters.AddWithValue("@Account", account);
                    cmd.Parameters.AddWithValue("@SN", sn);
                    cmd.Connection.Open();
                    cmd.CommandTimeout = 1200;
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            miner = new Miners();
                            miner.Id = GetDataValue<long>(dr, "Id");
                            miner.Address = GetDataValue<string>(dr, "Address");
                            miner.Account = GetDataValue<string>(dr, "Account");
                            miner.Type = GetDataValue<int>(dr, "Type");
                            miner.SN = GetDataValue<string>(dr, "SN");
                            miner.Status = GetDataValue<int>(dr, "Status");
                            miner.Timstamp = GetDataValue<long>(dr, "Timstamp");
                            miner.LastLoginTime = GetDataValue<long>(dr, "LastLoginTime");
                            miner.UnpaidReward = GetDataValue<long>(dr, "UnpaidReward");
                            miner.PaidReward = GetDataValue<long>(dr, "PaidReward");
                        }
                    }
                }
            }

            return miner;
        }

        public long GetUnPaidReward(string address)
        {
            const string SQL_STATEMENT =
                "SELECT SUM(UnpaidReward) AS SumReward " +
                "FROM Miners " +
                "WHERE Address = @Address;";

            long result = 0;
            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
                {
                    cmd.Parameters.AddWithValue("@Address", address);

                    cmd.Connection.Open();
                    cmd.CommandTimeout = 1200;
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            result = System.Convert.ToInt64(GetDataValue<decimal>(dr, "SumReward"));
                        }
                    }

                    cmd.Connection.Close();
                }

                con.Close();
            }

            return result;
        }

        public long GetPaidReward(string address)
        {
            const string SQL_STATEMENT =
                "SELECT SUM(PaidReward) AS SumReward " +
                "FROM Miners " +
                "WHERE Address = @Address;";

            long result = 0;
            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
                {
                    cmd.Parameters.AddWithValue("@Address", address);

                    cmd.Connection.Open();
                    cmd.CommandTimeout = 1200;
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            result = System.Convert.ToInt64(GetDataValue<decimal>(dr, "SumReward"));
                        }
                    }

                    cmd.Connection.Close();
                }

                con.Close();
            }

            return result;
        }

        public void UpdateSendReward(string address, long amount)
        {
            string SQL_STATEMENT =
                $"UPDATE Miners SET UnpaidReward = UnpaidReward - {amount}, PaidReward = PaidReward + {amount} WHERE Address = '{address}';";

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateDiscardedUnpaidReward(string tableName, string blockHash)
        {
            string SQL_STATEMENT =
                $"UPDATE Miners SET UnpaidReward = UnpaidReward - (SELECT ActualReward FROM {tableName} WHERE BlockHash='{blockHash}' AND MinerAddress=Miners.`Address`);";

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
