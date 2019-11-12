using OmniCoin.MiningPool.Entities;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace OmniCoin.MiningPool.Data
{
    public class HashRatesDac : DataAccessComponent
    {
        public void Insert(HashRates entity)
        {
            const string SQL_STATEMENT =
                "INSERT INTO HashRates " +
                "(Time, Hashes) " +
                "VALUES (@Time, @Hashes);";
            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Parameters.AddWithValue("@Time", entity.Time);
                cmd.Parameters.AddWithValue("@Hashes", entity.Hashes);

                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(long id)
        {
            const string SQL_STATEMENT =
                "DELETE FROM HashRates " +
                "WHERE Id = @Id;";
            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public List<HashRates> SelectAll()
        {
            const string SQL_STATEMENT =
                "SELECT Id, Time, Hashes " +
                "FROM HashRates;";

            List<HashRates> result = null;

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Connection.Open();
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    result = new List<HashRates>();

                    while (dr.Read())
                    {
                        HashRates hashRates = new HashRates();
                        hashRates.Id = GetDataValue<long>(dr, "Id");
                        hashRates.Time = GetDataValue<long>(dr, "Time");
                        hashRates.Hashes = GetDataValue<long>(dr, "Hashes");
                        
                        result.Add(hashRates);
                    }
                }
            }

            return result;
        }

        public HashRates SelectById(long id)
        {
            const string SQL_STATEMENT =
                "SELECT Id, Time, Hashes " +
                "FROM HashRates WHERE Id=@Id";

            HashRates hashRates = null;

            using (MySqlConnection conn = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Connection.Open();
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        hashRates = new HashRates();
                        hashRates.Id = GetDataValue<long>(dr, "Id");
                        hashRates.Time = GetDataValue<long>(dr, "Time");
                        hashRates.Hashes = GetDataValue<long>(dr, "Hashes");
                    }
                }
            }

            return hashRates;
        }

        public void Update(HashRates entity)
        {
            const string SQL_STATEMENT =
                "UPDATE HashRates " +
                "SET Time = @Time, Hashes = @Hashes " +
                "WHERE Id = @Id;";

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Parameters.AddWithValue("@Id", entity.Id);
                cmd.Parameters.AddWithValue("@Time", entity.Time);
                cmd.Parameters.AddWithValue("@Hashes", entity.Hashes);

                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public bool IsExisted(long id)
        {
            const string SQL_STATEMENT =
                "SELECT 1 " +
                "FROM HashRates " +
                "WHERE Id = @Id;";

            bool hasEntity = false;

            using (MySqlConnection con = new MySqlConnection(CacheConnectionString))
            using (MySqlCommand cmd = new MySqlCommand(SQL_STATEMENT, con))
            {
                cmd.Parameters.AddWithValue("@Id", id);

                cmd.Connection.Open();
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    hasEntity = dr.HasRows;
                }
            }

            return hasEntity;
        }
    }
}
