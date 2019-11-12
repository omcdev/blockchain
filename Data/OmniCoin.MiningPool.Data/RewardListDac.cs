using System.Data.SqlClient;
using System.Collections.Generic;
using OmniCoin.MiningPool.Entities;
using OmniCoin.Framework;
using MySql.Data.MySqlClient;
using System.Text;
using System.Linq;
using System;

namespace OmniCoin.MiningPool.Data
{
    public class RewardListDac : DataAccessComponent
    {
        /// <summary>
        /// 根据时间创建RewardList表，第一次写入的时候判断是否同一天，然后删除三个月前的数据表
        /// </summary>
        /// <param name="reward"></param>
        public void Insert(RewardList reward)
        {
            string network = Framework.GlobalParameters.IsTestnet ? "miningpooltest" : "miningpool";
            string tableName = "RewardList" + System.DateTime.Now.ToString("yyyyMMdd");
            StringBuilder SQL_STATEMENT = new StringBuilder();
            //创建表
            SQL_STATEMENT.Append($"CREATE TABLE IF NOT EXISTS `{tableName}`(");
            SQL_STATEMENT.Append("`Id` BIGINT PRIMARY KEY AUTO_INCREMENT COMMENT 'Id',");
            SQL_STATEMENT.Append("`BlockHash` VARCHAR(64) NOT NULL COMMENT '区块Hash',");
            SQL_STATEMENT.Append("`MinerAddress` VARCHAR(64) NOT NULL COMMENT '钱包地址',");
            SQL_STATEMENT.Append("`Hashes` BIGINT NOT NULL DEFAULT '0' COMMENT 'Hash个数',");
            SQL_STATEMENT.Append("`OriginalReward` BIGINT NOT NULL DEFAULT '0' COMMENT '原始奖励',");
            SQL_STATEMENT.Append("`ActualReward` BIGINT NOT NULL DEFAULT '0' COMMENT '实际奖励',");
            SQL_STATEMENT.Append("`Paid` INT NOT NULL DEFAULT '0' COMMENT '是否支付 0：未支付，1已支付',");
            SQL_STATEMENT.Append("`GenerateTime` BIGINT NOT NULL DEFAULT '0' COMMENT '生成时间时间戳',");
            SQL_STATEMENT.Append("`PaidTime` BIGINT NOT NULL DEFAULT '0' COMMENT '支付时间时间戳',");
            SQL_STATEMENT.Append("`TransactionHash` VARCHAR(64) NOT NULL COMMENT '交易Hash',");
            SQL_STATEMENT.Append("`Commission` BIGINT NOT NULL DEFAULT '0' COMMENT '提成奖励',");
            SQL_STATEMENT.Append("`IsCommissionProcessed` INT NOT NULL DEFAULT '0' COMMENT '提成是否发放',");
            SQL_STATEMENT.Append("`CommissionProcessedTime` BIGINT COMMENT '提成发放时间'");
            SQL_STATEMENT.Append(")ENGINE = InnoDB DEFAULT CHARSET = utf8 COMMENT = '奖励信息表';");
            //插入数据
            SQL_STATEMENT.Append($"INSERT INTO {tableName} ");
            SQL_STATEMENT.Append("(BlockHash, MinerAddress, Hashes, OriginalReward, ActualReward, Paid, GenerateTime, PaidTime, TransactionHash, IsCommissionProcessed, CommissionProcessedTime) ");
            SQL_STATEMENT.Append($"VALUES ('{reward.BlockHash}', '{reward.MinerAddress}', {reward.Hashes}, {reward.OriginalReward}, {reward.ActualReward}, {reward.Paid}, {reward.GenerateTime}, {reward.PaidTime}, '{reward.TransactionHash}', {reward.IsCommissionProcessed}, {reward.CommissionProcessedTime});");

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT.ToString(), conn))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(long id)
        {
            const string SQL_STATEMENT =
                "DELETE FROM RewardList " +
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

        public void DropExpireTables()
        {
            //删除过期的表
            string tableNames = "";
            string schme = GlobalParameters.IsTestnet ? "miningpooltest" : "miningpool";
            //先查询需要删除的表
            string queryTableName = $"SELECT GROUP_CONCAT(CONCAT({schme}, '.', table_name)) AS name FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA ='{schme}' AND CREATE_TIME< date_sub(NOW(), INTERVAL 3 MONTH) ORDER BY create_time DESC;";
            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(queryTableName, conn))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        tableNames = GetDataValue<string>(dr, "name");
                    }
                }
            }
            //删除表
            string dropTable = $"DROP TABLE {tableNames};";
            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(dropTable, conn))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(string hash)
        {
            const string SQL_STATEMENT =
                "DELETE FROM RewardList " +
                "WHERE BlockHash = @Hash;";

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Parameters.AddWithValue("@Hash", hash);
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        public List<RewardList> SelectAll()
        {
            const string SQL_STATEMENT =
                "SELECT Id, BlockHash, MinerAddress, Hashes, OriginalReward, ActualReward, Paid, GenerateTime, PaidTime, TransactionHash, IsCommissionProcessed, CommissionProcessedTime " +
                "FROM RewardList;";

            List<RewardList> result = null;

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    result = new List<RewardList>();

                    while (dr.Read())
                    {
                        RewardList reward = new RewardList();
                        reward.Id = GetDataValue<long>(dr, "Id");
                        reward.BlockHash = GetDataValue<string>(dr, "BlockHash");
                        reward.MinerAddress = GetDataValue<string>(dr, "MinerAddress");
                        reward.Hashes = GetDataValue<long>(dr, "Hashes");
                        reward.OriginalReward = GetDataValue<long>(dr, "OriginalReward");
                        reward.ActualReward = GetDataValue<long>(dr, "ActualReward");
                        reward.Paid = GetDataValue<int>(dr, "Paid");
                        reward.GenerateTime = GetDataValue<long>(dr, "GenerateTime");
                        reward.PaidTime = GetDataValue<long>(dr, "PaidTime");
                        reward.TransactionHash = GetDataValue<string>(dr, "TransactionHash");
                        reward.IsCommissionProcessed = GetDataValue<int>(dr, "IsCommissionProcessed");
                        reward.CommissionProcessedTime = GetDataValue<long?>(dr, "CommissionProcessedTime");

                        result.Add(reward);
                    }
                }
            }

            return result;
        }

        public RewardList SelectById(long id)
        {
            const string SQL_STATEMENT =
                "SELECT Id, BlockHash, MinerAddress, Hashes, OriginalReward, ActualReward, Paid, GenerateTime, PaidTime, TransactionHash, IsCommissionProcessed, CommissionProcessedTime " +
                "FROM RewardList WHERE Id=@Id;";

            RewardList reward = null;

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
                        reward = new RewardList();
                        reward.Id = GetDataValue<long>(dr, "Id");
                        reward.BlockHash = GetDataValue<string>(dr, "BlockHash");
                        reward.MinerAddress = GetDataValue<string>(dr, "MinerAddress");
                        reward.Hashes = GetDataValue<long>(dr, "Hashes");
                        reward.OriginalReward = GetDataValue<long>(dr, "OriginalReward");
                        reward.ActualReward = GetDataValue<long>(dr, "ActualReward");
                        reward.Paid = GetDataValue<int>(dr, "Paid");
                        reward.GenerateTime = GetDataValue<long>(dr, "GenerateTime");
                        reward.PaidTime = GetDataValue<long>(dr, "PaidTime");
                        reward.TransactionHash = GetDataValue<string>(dr, "TransactionHash");
                        reward.IsCommissionProcessed = GetDataValue<int>(dr, "IsCommissionProcessed");
                        reward.CommissionProcessedTime = GetDataValue<long?>(dr, "CommissionProcessedTime");
                    }
                }
            }

            return reward;
        }

        public RewardList SelectByHash(string hash)
        {
            const string SQL_STATEMENT =
                "SELECT Id, BlockHash, MinerAddress, Hashes, OriginalReward, ActualReward, Paid, GenerateTime, PaidTime, TransactionHash, IsCommissionProcessed, CommissionProcessedTime " +
                "FROM RewardList WHERE BlockHash=@Hash LIMIT 1;";

            RewardList reward = null;

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Parameters.AddWithValue("@Hash", hash);
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        reward = new RewardList();
                        reward.Id = GetDataValue<long>(dr, "Id");
                        reward.BlockHash = GetDataValue<string>(dr, "BlockHash");
                        reward.MinerAddress = GetDataValue<string>(dr, "MinerAddress");
                        reward.Hashes = GetDataValue<long>(dr, "Hashes");
                        reward.OriginalReward = GetDataValue<long>(dr, "OriginalReward");
                        reward.ActualReward = GetDataValue<long>(dr, "ActualReward");
                        reward.Paid = GetDataValue<int>(dr, "Paid");
                        reward.GenerateTime = GetDataValue<long>(dr, "GenerateTime");
                        reward.PaidTime = GetDataValue<long>(dr, "PaidTime");
                        reward.TransactionHash = GetDataValue<string>(dr, "TransactionHash");
                        reward.IsCommissionProcessed = GetDataValue<int>(dr, "IsCommissionProcessed");
                        reward.CommissionProcessedTime = GetDataValue<long?>(dr, "CommissionProcessedTime");
                    }
                }
            }

            return reward;
        }

        public List<RewardList> GetListByHash(string tableName, string blockHash)
        {
            string SQL_STATEMENT =
                "SELECT Id, BlockHash, MinerAddress, Hashes, OriginalReward, ActualReward, Paid, GenerateTime, PaidTime, TransactionHash, IsCommissionProcessed, CommissionProcessedTime " +
                $"FROM {tableName} WHERE BlockHash='{blockHash}';";

            List<RewardList> list = null;

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    list = new List<RewardList>();
                    while (dr.Read())
                    {
                        RewardList reward = new RewardList();
                        reward.Id = GetDataValue<long>(dr, "Id");
                        reward.BlockHash = GetDataValue<string>(dr, "BlockHash");
                        reward.MinerAddress = GetDataValue<string>(dr, "MinerAddress");
                        reward.Hashes = GetDataValue<long>(dr, "Hashes");
                        reward.OriginalReward = GetDataValue<long>(dr, "OriginalReward");
                        reward.ActualReward = GetDataValue<long>(dr, "ActualReward");
                        reward.Paid = GetDataValue<int>(dr, "Paid");
                        reward.GenerateTime = GetDataValue<long>(dr, "GenerateTime");
                        reward.PaidTime = GetDataValue<long>(dr, "PaidTime");
                        reward.TransactionHash = GetDataValue<string>(dr, "TransactionHash");
                        reward.IsCommissionProcessed = GetDataValue<int>(dr, "IsCommissionProcessed");
                        reward.CommissionProcessedTime = GetDataValue<long?>(dr, "CommissionProcessedTime");

                        list.Add(reward);
                    }
                }
            }

            return list;
        }

        public void UpdatePaid(long id, int paid, string transactionHash)
        {
            const string SQL_STATEMENT =
                "UPDATE RewardList " +
                "SET Paid = @Paid, PaidTime = @PaidTime, TransactionHash = @TransactionHash " +
                "WHERE Id = @Id;";

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Paid", paid);
                cmd.Parameters.AddWithValue("@PaidTime", Framework.Time.EpochTime);
                cmd.Parameters.AddWithValue("@TransactionHash", transactionHash);

                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdatePaid(string tableName, string address, int paid, string transactionHash, string blockHashes)
        {
            string SQL_STATEMENT =
                $"UPDATE {tableName} SET Paid = {paid}, PaidTime = {Time.EpochTime}, TransactionHash = '{transactionHash}' WHERE Paid = 0 AND MinerAddress = '{address}' AND BlockHash IN({blockHashes});";

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }
        public void UpdatePaidByAddresses(string tableName, string address, int paid, string transactionHash, string blockHashes)
        {
            string SQL_STATEMENT =
                $"UPDATE {tableName} SET Paid = {paid}, PaidTime = {Time.EpochTime}, TransactionHash = '{transactionHash}' WHERE Paid = 0 AND MinerAddress IN ({address}) AND BlockHash IN({blockHashes});";

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateNullPaid(string tableName, string address, int paid, string blockHashes)
        {
            string SQL_STATEMENT =
                $"UPDATE {tableName} SET Paid = {paid}, PaidTime = {Time.EpochTime}, TransactionHash = '' WHERE MinerAddress = '{address}' AND ActualReward = 0 AND BlockHash IN({blockHashes});";

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdatePaid(string tableName, string hash, int paid, long height)
        {
            string SQL_STATEMENT =
                $"UPDATE { tableName} SET Paid = @Paid, PaidTime = @PaidTime WHERE BlockHash = @Hash;" +
                $"UPDATE { tableName} SET Paid = 2 WHERE BlockHash IN(SELECT Hash FROM Blocks WHERE Height <= { height}  AND IsDiscarded = 1 AND Confirmed = 0);";

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Parameters.AddWithValue("@TableName", tableName);
                cmd.Parameters.AddWithValue("@Hash", hash);
                cmd.Parameters.AddWithValue("@Paid", paid);
                cmd.Parameters.AddWithValue("@PaidTime", Time.EpochTime);

                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        public bool IsExisted(long id)
        {
            const string SQL_STATEMENT =
                "SELECT 1 " +
                "FROM RewardList " +
                "WHERE Id = @Id;";

            bool hasReward = false;

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Parameters.AddWithValue("@Id", id);

                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    hasReward = dr.HasRows;
                }
            }

            return hasReward;
        }

        public bool IsExisted(string hash)
        {
            const string SQL_STATEMENT =
                "SELECT 1 " +
                "FROM RewardList " +
                "WHERE BlockHash = @Hash;";

            bool hasReward = false;

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Parameters.AddWithValue("@Hash", hash);

                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    hasReward = dr.HasRows;
                }
            }

            return hasReward;
        }

        public long GetUnPaidReward(string address, int payStatus)
        {
            const string SQL_STATEMENT =
                "SELECT SUM(ActualReward) AS SumReward " +
                "FROM RewardList " +
                "WHERE MinerAddress = @MinerAddress AND Paid = @Paid AND BlockHash IN (SELECT Hash FROM Blocks WHERE Confirmed = 1 AND IsDiscarded = 0);";

            long result = 0;
            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Parameters.AddWithValue("@MinerAddress", address);
                cmd.Parameters.AddWithValue("@Paid", payStatus);

                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        result = GetDataValue<long>(dr, "SumReward");
                    }
                }
            }

            return result;
        }

        public long GetPaidReward(string address, int payStatus)
        {
            const string SQL_STATEMENT =
                "SELECT SUM(ActualReward) AS SumReward " +
                "FROM RewardList " +
                "WHERE MinerAddress = @MinerAddress AND Paid = @Paid;";

            long result = 0;

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Parameters.AddWithValue("@MinerAddress", address);
                cmd.Parameters.AddWithValue("@Paid", payStatus);

                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {

                    while (dr.Read())
                    {
                        result = GetDataValue<long>(dr, "SumReward");
                    }
                }
            }

            return result;
        }

        public List<RewardList> GetAllUnPaidReward(string tableName, string blockHashes)
        {
            string SQL_STATEMENT =
                $"SELECT Id, BlockHash, MinerAddress, Hashes, OriginalReward, ActualReward, Paid, GenerateTime, PaidTime, TransactionHash, IsCommissionProcessed, CommissionProcessedTime FROM {tableName} WHERE Paid = 0 AND BlockHash IN ({blockHashes});";

            List<RewardList> result = null;

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    result = new List<RewardList>();

                    while (dr.Read())
                    {
                        RewardList reward = new RewardList();
                        reward.Id = GetDataValue<long>(dr, "Id");
                        reward.BlockHash = GetDataValue<string>(dr, "BlockHash");
                        reward.MinerAddress = GetDataValue<string>(dr, "MinerAddress");
                        reward.Hashes = GetDataValue<long>(dr, "Hashes");
                        reward.OriginalReward = GetDataValue<long>(dr, "OriginalReward");
                        reward.ActualReward = GetDataValue<long>(dr, "ActualReward");
                        reward.Paid = GetDataValue<int>(dr, "Paid");
                        reward.GenerateTime = GetDataValue<long>(dr, "GenerateTime");
                        reward.PaidTime = GetDataValue<long>(dr, "PaidTime");
                        reward.TransactionHash = GetDataValue<string>(dr, "TransactionHash");
                        reward.IsCommissionProcessed = GetDataValue<int>(dr, "IsCommissionProcessed");
                        reward.CommissionProcessedTime = GetDataValue<long?>(dr, "CommissionProcessedTime");

                        result.Add(reward);
                    }
                }
            }

            return result;
        }

        public Dictionary<string, long> GetAllUnPaidRewardGroup(string tableName, string blockHashes)
        {
            string SQL_STATEMENT =
                $"SELECT MinerAddress, SUM(ActualReward) FROM {tableName} WHERE Paid = 0 AND BlockHash IN ({blockHashes}) GROUP BY MinerAddress;";

            Dictionary<string, long> result = null;

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    result = new Dictionary<string, long>();

                    while (dr.Read())
                    {
                        result.Add(dr.GetString(0), dr.GetInt64(1));
                    }
                }
            }

            return result;
        }



        /// <summary>
        /// 固定地址的发奖励,测试发消息队列专用
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="blockHashes"></param>
        /// <param name="addresses"></param>
        /// <returns></returns>
        public List<RewardList> GetAllUnPaidRewardByAddresses(string tableName, string blockHashes, string addresses)
        {
            string SQL_STATEMENT =
                $"SELECT Id, BlockHash, MinerAddress, Hashes, OriginalReward, ActualReward, Paid, GenerateTime, PaidTime, TransactionHash, IsCommissionProcessed, CommissionProcessedTime FROM {tableName} WHERE Paid = 0 AND BlockHash IN ({blockHashes}) AND MinerAddress IN ({addresses});";

            List<RewardList> result = null;

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    result = new List<RewardList>();

                    while (dr.Read())
                    {
                        RewardList reward = new RewardList();
                        reward.Id = GetDataValue<long>(dr, "Id");
                        reward.BlockHash = GetDataValue<string>(dr, "BlockHash");
                        reward.MinerAddress = GetDataValue<string>(dr, "MinerAddress");
                        reward.Hashes = GetDataValue<long>(dr, "Hashes");
                        reward.OriginalReward = GetDataValue<long>(dr, "OriginalReward");
                        reward.ActualReward = GetDataValue<long>(dr, "ActualReward");
                        reward.Paid = GetDataValue<int>(dr, "Paid");
                        reward.GenerateTime = GetDataValue<long>(dr, "GenerateTime");
                        reward.PaidTime = GetDataValue<long>(dr, "PaidTime");
                        reward.TransactionHash = GetDataValue<string>(dr, "TransactionHash");
                        reward.IsCommissionProcessed = GetDataValue<int>(dr, "IsCommissionProcessed");
                        reward.CommissionProcessedTime = GetDataValue<long?>(dr, "CommissionProcessedTime");

                        result.Add(reward);
                    }
                }
            }

            return result;
        }

        public List<RewardList> GetUnPaidRewardBlock()
        {
            const string SQL_STATEMENT =
                "SELECT Id, BlockHash, MinerAddress, Hashes, OriginalReward, ActualReward, Paid, GenerateTime, PaidTime, TransactionHash, IsCommissionProcessed, CommissionProcessedTime " +
                "FROM RewardList " +
                "WHERE Paid = 0 AND BlockHash = (SELECT BlockHash FROM RewardList WHERE Paid = 0 LIMIT 1) ORDER BY Id;";

            List<RewardList> result = null;

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    result = new List<RewardList>();

                    while (dr.Read())
                    {
                        RewardList reward = new RewardList();
                        reward.Id = GetDataValue<long>(dr, "Id");
                        reward.BlockHash = GetDataValue<string>(dr, "BlockHash");
                        reward.MinerAddress = GetDataValue<string>(dr, "MinerAddress");
                        reward.Hashes = GetDataValue<long>(dr, "Hashes");
                        reward.OriginalReward = GetDataValue<long>(dr, "OriginalReward");
                        reward.ActualReward = GetDataValue<long>(dr, "ActualReward");
                        reward.Paid = GetDataValue<int>(dr, "Paid");
                        reward.GenerateTime = GetDataValue<long>(dr, "GenerateTime");
                        reward.PaidTime = GetDataValue<long>(dr, "PaidTime");
                        reward.TransactionHash = GetDataValue<string>(dr, "TransactionHash");
                        reward.IsCommissionProcessed = GetDataValue<int>(dr, "IsCommissionProcessed");
                        reward.CommissionProcessedTime = GetDataValue<long?>(dr, "CommissionProcessedTime");

                        result.Add(reward);
                    }
                }
            }

            return result;
        }

        public List<RewardList> GetCustomUnPaidReward(int count)
        {
            string SQL_STATEMENT =
                "PREPARE s1 FROM 'SELECT Id, BlockHash, MinerAddress, Hashes, OriginalReward, ActualReward, Paid, GenerateTime, PaidTime, TransactionHash, IsCommissionProcessed, CommissionProcessedTime " +
                "FROM RewardList " +
                "WHERE Paid = 0 AND BlockHash IN(SELECT Hash FROM Blocks WHERE Confirmed = 1 AND IsDiscarded = 0) ORDER BY Id LIMIT ?';" +
                "SET @Count = 3;" +
                "EXECUTE s1 USING @Count;";

            List<RewardList> result = null;

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    result = new List<RewardList>();

                    while (dr.Read())
                    {
                        RewardList reward = new RewardList();
                        reward.Id = GetDataValue<long>(dr, "Id");
                        reward.BlockHash = GetDataValue<string>(dr, "BlockHash");
                        reward.MinerAddress = GetDataValue<string>(dr, "MinerAddress");
                        reward.Hashes = GetDataValue<long>(dr, "Hashes");
                        reward.OriginalReward = GetDataValue<long>(dr, "OriginalReward");
                        reward.ActualReward = GetDataValue<long>(dr, "ActualReward");
                        reward.Paid = GetDataValue<int>(dr, "Paid");
                        reward.GenerateTime = GetDataValue<long>(dr, "GenerateTime");
                        reward.PaidTime = GetDataValue<long>(dr, "PaidTime");
                        reward.TransactionHash = GetDataValue<string>(dr, "TransactionHash");
                        reward.IsCommissionProcessed = GetDataValue<int>(dr, "IsCommissionProcessed");
                        reward.CommissionProcessedTime = GetDataValue<long?>(dr, "CommissionProcessedTime");

                        result.Add(reward);
                    }
                }
            }

            return result;
        }

        public long GetActualReward(string address, string blockHash)
        {
            const string SQL_STATEMENT =
                "SELECT ActualReward " +
                "FROM RewardList " +
                "WHERE BlockHash = @BlockHash AND MinerAddress = @MinerAddress LIMIT 1;";
            long result = 0L;
            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Parameters.AddWithValue("@BlockHash", blockHash);
                cmd.Parameters.AddWithValue("@MinerAddress", address);
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        result = GetDataValue<long>(dr, "ActualReward");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 返回当前所有未到期的存币记录
        /// </summary>
        /// <returns></returns>
        public List<DepositList> GetAllNotExpiredDeposit()
        {
            //存币至少1个币才计算在内
            string SQL_STATEMENT = $"SELECT * FROM depositlist WHERE IsExpired=0 AND {Time.EpochTime} < ExpireTime   AND Amount > 99999999;";

            List<DepositList> result = null;

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    result = new List<DepositList>();

                    while (dr.Read())
                    {
                        DepositList deposit = new DepositList();
                        deposit.Id = GetDataValue<long>(dr, "Id");
                        deposit.TransactionHash = GetDataValue<string>(dr, "TransactionHash");
                        deposit.Amount = GetDataValue<long>(dr, "Amount");
                        deposit.ExpireTime = GetDataValue<long>(dr, "ExpireTime");
                        deposit.Address = GetDataValue<string>(dr, "Address");
                        deposit.IsExpired = GetDataValue<int>(dr, "IsExpired");
                        result.Add(deposit);
                    }
                }
            }

            return result;            
        }

        public void InsertDeposit(List<DepositList> deposits)
        {   if(deposits == null || !deposits.Any())
            {
                return ;
            }
            StringBuilder SQL_STATEMENT = new StringBuilder();      
            
            foreach(var x in deposits)
            {
                SQL_STATEMENT.AppendLine($"INSERT INTO depositlist (TransactionHash, Amount, ExpireTime, Address, IsExpired) VALUES ('{x.TransactionHash}', {x.Amount}, {x.ExpireTime}, '{x.Address}', {x.IsExpired});");
            }          

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            {
                conn.Open();
                MySqlTransaction transaction = conn.BeginTransaction();
                MySqlCommand cmd = conn.CreateCommand();
                cmd.Transaction = transaction;
                try
                {
                    //插入Blocks表
                    cmd.CommandText = SQL_STATEMENT.ToString();
                    int x = cmd.ExecuteNonQuery();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception en)
                    {
                        throw new Exception(en.ToString());
                    }
                    throw new Exception(ex.ToString());
                }
            }
        }

        /// <summary>
        /// 更新存币记录表 此刻已到期的存币记录的状态为已到期
        /// </summary>
        public void UpdateDepositStatus()
        {
            string SQL_STATEMENT =
                $"UPDATE depositlist SET IsExpired = 1  WHERE  ExpireTime < {Time.EpochTime};";

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
