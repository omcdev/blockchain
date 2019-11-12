using OmniCoin.MiningPool.Entities;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace OmniCoin.MiningPool.Data
{
    public class BlocksDac : DataAccessComponent
    {
        public void Insert(Blocks block, List<RewardList> rewardLists, DateTime now)
        {
            string tableName = "RewardList" + now.ToString("yyyyMMdd");
            StringBuilder sb = new StringBuilder();
            //创建表
            sb.Append($"CREATE TABLE IF NOT EXISTS `{tableName}`(");
            sb.Append("`Id` BIGINT PRIMARY KEY AUTO_INCREMENT COMMENT 'Id',");
            sb.Append("`BlockHash` VARCHAR(64) NOT NULL COMMENT '区块Hash',");
            sb.Append("`MinerAddress` VARCHAR(64) NOT NULL COMMENT '钱包地址',");
            sb.Append("`Hashes` BIGINT NOT NULL DEFAULT '0' COMMENT 'Hash个数',");
            sb.Append("`OriginalReward` BIGINT NOT NULL DEFAULT '0' COMMENT '原始奖励',");
            sb.Append("`ActualReward` BIGINT NOT NULL DEFAULT '0' COMMENT '实际奖励',");
            sb.Append("`Paid` INT NOT NULL DEFAULT '0' COMMENT '是否支付 0：未支付，1已支付',");
            sb.Append("`GenerateTime` BIGINT NOT NULL DEFAULT '0' COMMENT '生成时间时间戳',");
            sb.Append("`PaidTime` BIGINT NOT NULL DEFAULT '0' COMMENT '支付时间时间戳',");
            sb.Append("`TransactionHash` VARCHAR(64) NOT NULL COMMENT '交易Hash',");
            sb.Append("`Commission` BIGINT NOT NULL DEFAULT '0' COMMENT '提成奖励',");
            sb.Append("`IsCommissionProcessed` INT NOT NULL DEFAULT '0' COMMENT '提成是否发放',");
            sb.Append("`CommissionProcessedTime` BIGINT COMMENT '提成发放时间',");
            sb.Append("`DepositTotalAmount` BIGINT NOT NULL DEFAULT '0' COMMENT '总存币金额',");
            sb.Append("`AddressDepositTotalAmount` BIGINT NOT NULL DEFAULT '0' COMMENT '用户存币总金额',");
            sb.Append("`RewardType` INT NOT NULL DEFAULT '0' COMMENT '奖励类型,0 - 矿工, 1 - 超级节点，2 - 存币利息',");
            sb.Append("`DepositTransactionHash` VARCHAR(64) NOT NULL COMMENT '存币交易Hash',");            
            sb.Append("INDEX `BlockHash` (`BlockHash`),");
            sb.Append("INDEX `MinerAddress` (`MinerAddress`),");
            sb.Append("INDEX `Paid` (`Paid`),");
            sb.Append("INDEX `TransactionHash` (`TransactionHash`),");
            sb.Append("INDEX `RewardType` (`RewardType`),");
            sb.Append("INDEX `DepositTransactionHash` (`DepositTransactionHash`)");
            sb.Append(")ENGINE = InnoDB DEFAULT CHARSET = utf8 COMMENT = '奖励信息表';");
            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            {
                conn.Open();
                MySqlTransaction transaction = conn.BeginTransaction();
                MySqlCommand cmd = conn.CreateCommand();
                cmd.Transaction = transaction;
                try
                {
                    //插入Blocks表
                    cmd.CommandText = $"INSERT INTO Blocks(Hash, Height, Timstamp, Generator, Nonce, TotalReward, TotalHashes, Confirmed, IsDiscarded, IsRewardSend) VALUES ('{block.Hash}', {block.Height}, {block.Timstamp}, '{block.Generator}', {block.Nonce}, {block.TotalReward}, {block.TotalHash}, {block.Confirmed}, {block.IsDiscarded}, {block.IsRewardSend});"; ;
                    int x = cmd.ExecuteNonQuery();
                    //根据日期创建RewardList+日期表
                    cmd.CommandText = sb.ToString();
                    int y = cmd.ExecuteNonQuery();
                    //
                    //插入RewardList表中
                    foreach (var item in rewardLists)
                    {
                        cmd.CommandText = $"INSERT INTO {tableName}(BlockHash, MinerAddress, Hashes, OriginalReward, ActualReward, Paid, GenerateTime, PaidTime, TransactionHash, IsCommissionProcessed, CommissionProcessedTime,DepositTotalAmount,AddressDepositTotalAmount,RewardType,DepositTransactionHash) VALUES ('{item.BlockHash}', '{item.MinerAddress}', {item.Hashes}, {item.OriginalReward}, {item.ActualReward}, {item.Paid}, {item.GenerateTime}, {item.PaidTime}, '{item.TransactionHash}', {item.IsCommissionProcessed}, {item.CommissionProcessedTime},{item.DepositTotalAmount},{item.AddressDepositTotalAmount},{item.RewardType},'{item.DepositTransactionHash}')";
                        cmd.ExecuteNonQuery();
                        //更新Miners表的UnpaidReward和PaidReward
                        cmd.CommandText = $"UPDATE Miners SET UnpaidReward = UnpaidReward + {item.ActualReward} WHERE Address = '{item.MinerAddress}'";
                        cmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
                catch(Exception ex)
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

        public void Delete(long id)
        {
            const string SQL_STATEMENT =
                "DELETE FROM Blocks " +
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

        public void Delete(string hash)
        {
            const string SQL_STATEMENT =
                "DELETE FROM Blocks " +
                "WHERE Hash = @Hash;";

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Parameters.AddWithValue("@Hash", hash);
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        public List<Blocks> SelectAll()
        {
            const string SQL_STATEMENT =
                "SELECT Id, Hash, Height, Timstamp, Generator, Nonce, TotalReward, TotalHashes, Confirmed, IsDiscarded, IsRewardSend " +
                "FROM Blocks;";

            List<Blocks> result = null;

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    result = new List<Blocks>();

                    while (dr.Read())
                    {
                        Blocks block = new Blocks();
                        block.Id = GetDataValue<long>(dr, "Id");
                        block.Hash = GetDataValue<string>(dr, "Hash");
                        block.Height = GetDataValue<long>(dr, "Height");
                        block.Timstamp = GetDataValue<long>(dr, "Timstamp");
                        block.Generator = GetDataValue<string>(dr, "Generator");
                        block.Nonce = GetDataValue<long>(dr, "Nonce");
                        block.TotalReward = GetDataValue<long>(dr, "TotalReward");
                        block.TotalHash = GetDataValue<long>(dr, "TotalHashes");
                        block.Confirmed = GetDataValue<int>(dr, "Confirmed");
                        block.IsDiscarded = GetDataValue<int>(dr, "IsDiscarded");
                        block.IsRewardSend = GetDataValue<int>(dr, "IsRewardSend");

                        result.Add(block);
                    }
                }
            }

            return result;
        }

        public Blocks SelectById(long id)
        {
            const string SQL_STATEMENT =
                "SELECT Id, Hash, Height, Timstamp, Generator, Nonce, TotalReward, TotalHashes, Confirmed, IsDiscarded, IsRewardSend " +
                "FROM Blocks WHERE Id=@Id;";

            Blocks block = null;

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
                        block = new Blocks();
                        block.Id = GetDataValue<long>(dr, "Id");
                        block.Hash = GetDataValue<string>(dr, "Hash");
                        block.Height = GetDataValue<long>(dr, "Height");
                        block.Timstamp = GetDataValue<long>(dr, "Timstamp");
                        block.Generator = GetDataValue<string>(dr, "Generator");
                        block.Nonce = GetDataValue<long>(dr, "Nonce");
                        block.TotalReward = GetDataValue<long>(dr, "TotalReward");
                        block.TotalHash = GetDataValue<long>(dr, "TotalHashes");
                        block.Confirmed = GetDataValue<int>(dr, "Confirmed");
                        block.IsDiscarded = GetDataValue<int>(dr, "IsDiscarded");
                        block.IsRewardSend = GetDataValue<int>(dr, "IsRewardSend");
                    }
                }
            }

            return block;
        }

        public Blocks SelectByHash(string hash)
        {
            const string SQL_STATEMENT =
                "SELECT Id, Hash, Height, Timstamp, Generator, Nonce, TotalReward, TotalHashes, Confirmed, IsDiscarded, IsRewardSend " +
                "FROM Blocks WHERE Hash=@Hash LIMIT 1;";

            Blocks block = null;

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
                        block = new Blocks();
                        block.Id = GetDataValue<long>(dr, "Id");
                        block.Hash = GetDataValue<string>(dr, "Hash");
                        block.Height = GetDataValue<long>(dr, "Height");
                        block.Timstamp = GetDataValue<long>(dr, "Timstamp");
                        block.Generator = GetDataValue<string>(dr, "Generator");
                        block.Nonce = GetDataValue<long>(dr, "Nonce");
                        block.TotalReward = GetDataValue<long>(dr, "TotalReward");
                        block.TotalHash = GetDataValue<long>(dr, "TotalHashes");
                        block.Confirmed = GetDataValue<int>(dr, "Confirmed");
                        block.IsDiscarded = GetDataValue<int>(dr, "IsDiscarded");
                        block.IsRewardSend = GetDataValue<int>(dr, "IsRewardSend");
                    }
                }
            }

            return block;
        }

        public void UpdateConfirmed(long id, int confirmed, int isDiscarded)
        {
            const string SQL_STATEMENT =
                "UPDATE Blocks " +
                "SET Confirmed = @Confirmed, IsDiscarded = @IsDiscarded " +
                "WHERE Id = @Id AND Confirmed = 0;";

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Confirmed", confirmed);
                cmd.Parameters.AddWithValue("@IsDiscarded", isDiscarded);

                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateConfirmed(string hash, int confirmed, int isDiscarded)
        {
            const string SQL_STATEMENT =
                "UPDATE Blocks " +
                "SET Confirmed = @Confirmed, IsDiscarded = @IsDiscarded " +
                "WHERE Hash = @Hash AND Confirmed = 0;";

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Parameters.AddWithValue("@Hash", hash);
                cmd.Parameters.AddWithValue("@Confirmed", confirmed);
                cmd.Parameters.AddWithValue("@IsDiscarded", isDiscarded);

                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateFailBlock(long height)
        {
            const string SQL_STATEMENT =
                "UPDATE Blocks " +
                "SET IsDiscarded = 1 " +
                "WHERE Height <= @Height AND IsDiscarded = 0 AND Confirmed = 0;";
                /*
                "UPDATE RewardList SET Paid = 2 " +
                "WHERE BlockHash IN (SELECT Hash FROM Blocks WHERE Height <= @Height AND IsDiscarded = 1 AND Confirmed = 0);";
                */

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Parameters.AddWithValue("@Height", height);

                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        public bool IsExisted(long id)
        {
            const string SQL_STATEMENT =
                "SELECT 1 " +
                "FROM Blocks " +
                "WHERE Id = @Id;";

            bool hasBlock = false;

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Parameters.AddWithValue("@Id", id);

                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    hasBlock = dr.HasRows;
                }
            }

            return hasBlock;
        }

        public bool IsExisted(string hash)
        {
            const string SQL_STATEMENT =
                "SELECT 1 " +
                "FROM Blocks " +
                "WHERE Hash = @Hash;";

            bool hasBlock = false;

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Parameters.AddWithValue("@Hash", hash);

                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    hasBlock = dr.HasRows;
                }
            }

            return hasBlock;
        }

        public List<string> GetAppointedHash(long height)
        {
            const string SQL_STATEMENT =
                "SELECT Hash " +
                "FROM Blocks " +
                "WHERE Height <= @Height AND Confirmed = 0;";

            List<string> result = null;
            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Parameters.AddWithValue("@Height", height);
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    result = new List<string>();
                    while (dr.Read())
                    {
                        Blocks block = new Blocks();
                        block.Hash = GetDataValue<string>(dr, "Hash");

                        result.Add(block.Hash);
                    }
                }
            }

            return result;
        }

        public List<Blocks> GetAllUnRewardBlocks(long? timestamp)
        {
            string sql =
                "SELECT Id, Hash, Height, Timstamp, Generator, Nonce, TotalReward, TotalHashes, Confirmed, IsDiscarded, IsRewardSend " +
                "FROM Blocks WHERE IsRewardSend = 0 AND Confirmed = 1 {0};";

            if(timestamp != null)
            {
                sql = string.Format(sql, $"AND Timstamp <= {timestamp.Value}");
            }
            else
            {
                sql = string.Format(sql, "");
            }

            List<Blocks> result = null;

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(sql, conn))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 9600;
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    result = new List<Blocks>();

                    while (dr.Read())
                    {
                        Blocks block = new Blocks();
                        block.Id = GetDataValue<long>(dr, "Id");
                        block.Hash = GetDataValue<string>(dr, "Hash");
                        block.Height = GetDataValue<long>(dr, "Height");
                        block.Timstamp = GetDataValue<long>(dr, "Timstamp");
                        block.Generator = GetDataValue<string>(dr, "Generator");
                        block.Nonce = GetDataValue<long>(dr, "Nonce");
                        block.TotalReward = GetDataValue<long>(dr, "TotalReward");
                        block.TotalHash = GetDataValue<long>(dr, "TotalHashes");
                        block.Confirmed = GetDataValue<int>(dr, "Confirmed");
                        block.IsDiscarded = GetDataValue<int>(dr, "IsDiscarded");
                        block.IsRewardSend = GetDataValue<int>(dr, "IsRewardSend");

                        result.Add(block);
                    }
                }
            }

            return result;
        }

        public void UpdateAllSendStatus(string tableName, string blockHashes)
        {
            //string SQL_STATMENT = $"UPDATE Blocks SET IsRewardSend = 1 WHERE BlockHash='{blockHashes}' AND IF(EXISTS(SELECT * FROM rewardlist20181026 WHERE BlockHash='23E952200762B2034188C82F6489782A0834359569AFE099BF0CFECF92F58E7C' AND Paid = 0), 1, 0) = 0;";
            /*
            string SQL_STATEMENT = $"UPDATE Blocks SET IsRewardSend = 1 WHERE `Hash` IN(SELECT BlockHash FROM {tableName} WHERE Paid = 1 AND BlockHash IN({blockHashes}));";

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Connection.Open();
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
            */
            if (blockHashes.Contains(","))
            {
                string[] blockhash = blockHashes.Split(',');
                foreach(string item in blockhash)
                {
                    string SQL_STATEMENT = $"UPDATE Blocks SET IsRewardSend = 1 WHERE `Hash`= {item} AND Confirmed = 1 AND IsDiscarded = 0 AND IsRewardSend = 0 AND IF(EXISTS(SELECT * FROM {tableName} WHERE BlockHash = {item} AND Paid = 0 AND BlockHash NOT IN(SELECT t.`Hash` FROM(SELECT `Hash` FROM Blocks WHERE IsDiscarded = 1)t)), 1, 0) = 0;";
                    using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
                    using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
                    {
                        cmd.Connection.Open();
                        cmd.CommandTimeout = 1200;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                string SQL_STATEMENT = $"UPDATE Blocks SET IsRewardSend = 1 WHERE `Hash`= {blockHashes} AND Confirmed = 1 AND IsDiscarded = 0 AND IsRewardSend = 0 AND IF(EXISTS(SELECT * FROM {tableName} WHERE BlockHash = {blockHashes} AND Paid = 0 AND BlockHash NOT IN(SELECT t.`Hash` FROM(SELECT `Hash` FROM Blocks WHERE IsDiscarded = 1)t)), 1, 0) = 0;";
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
}
