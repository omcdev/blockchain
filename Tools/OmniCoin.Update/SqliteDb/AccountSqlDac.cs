


using OmniCoin.Entities;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Data;

namespace OmniCoin.Update.SqliteDb
{
    public class SqlDac
    {
        protected static T GetDataValue<T>(IDataReader dr, string columnName)
        {
            int i = dr.GetOrdinal(columnName);
            var mydr = (Microsoft.Data.Sqlite.SqliteDataReader)dr;
            if (!dr.IsDBNull(i))
                return mydr.GetFieldValue<T>(i);
            else
                return default(T);
        }

        public virtual List<Account> SelectAccountBook()
        {
            const string SQL_STATEMENT =
                "SELECT Id, PrivateKey, PublicKey, Balance, IsDefault, WatchedOnly, Timestamp, Tag " +
                "FROM Accounts;";

            List<Account> result = null;

            using (SqliteConnection con = new SqliteConnection(SqlDb.ConnectionString))
            using (SqliteCommand cmd = new SqliteCommand(SQL_STATEMENT, con))
            {
                cmd.Connection.Open();
                //con.SynchronousNORMAL();
                using (SqliteDataReader dr = cmd.ExecuteReader())
                {
                    result = new List<Account>();

                    while (dr.Read())
                    {
                        Account account = new Account();
                        account.Id = dr.GetString(0);
                        account.PrivateKey = dr.GetString(1);
                        account.PublicKey = dr.GetString(2);
                        account.Balance = dr.GetInt64(3);
                        account.IsDefault = dr.GetBoolean(4);
                        account.WatchedOnly = dr.GetBoolean(5);
                        account.Timestamp = dr.GetInt64(6);
                        account.Tag = dr.IsDBNull(7) ? "" : dr.GetString(7);

                        result.Add(account);
                    }
                }
            }

            return result;
        }

        public virtual List<AddressBookItem> SelectAddressBook()
        {
            const string SQL_STATEMENT =
                "SELECT * " +
                "FROM AddressBook;";

            List<AddressBookItem> result = null;

            using (SqliteConnection con = new SqliteConnection(SqlDb.ConnectionString))
            using (SqliteCommand cmd = new SqliteCommand(SQL_STATEMENT, con))
            {
                cmd.Connection.Open();
                //con.SynchronousNORMAL();
                using (SqliteDataReader dr = cmd.ExecuteReader())
                {
                    result = new List<AddressBookItem>();

                    while (dr.Read())
                    {
                        AddressBookItem item = new AddressBookItem();
                        item.Id = GetDataValue<long>(dr, "Id");
                        item.Address = GetDataValue<string>(dr, "Address");
                        item.Tag = GetDataValue<string>(dr, "Tag");
                        item.Timestamp = GetDataValue<long>(dr, "Timestamp");

                        result.Add(item);
                    }
                }
            }

            return result;
        }

        public virtual Setting GetSetting()
        {
            const string SQL_STATEMENT =
                "SELECT Confirmations, FeePerKB, Encrypt, PassCiphertext " +
                "FROM Settings " +
                "LIMIT 1;";

            Setting setting = null;

            using (SqliteConnection con = new SqliteConnection(SqlDb.ConnectionString))
            using (SqliteCommand cmd = new SqliteCommand(SQL_STATEMENT, con))
            {
                cmd.Connection.Open();
                //con.SynchronousNORMAL();
                using (SqliteDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        setting = new Setting();
                        setting.Confirmations = dr.GetInt64(0);
                        setting.FeePerKB = dr.GetInt64(1);
                        setting.Encrypt = dr.GetBoolean(2);
                        setting.PassCiphertext = dr.IsDBNull(3) ? "" : dr.GetString(3);
                    }
                }
            }

            if (setting == null)
                setting = new Setting() { Confirmations = 7, Encrypt = false, FeePerKB = 100000, PassCiphertext = "" };

            return setting;
        }
    }
}
