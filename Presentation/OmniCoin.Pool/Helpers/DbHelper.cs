


using OmniCoin.Framework;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OmniCoin.Pool.Helpers
{
    public class Db<T> where T : class
    {
        public Db() { }
        public Db(T va)
        {
            Value = va;
        }
        public long Id { get; set; }
        public T Value { get; set; }
    }

    public class DataType
    {
        public const string ReceiveType = "Receive";
        public const string CommandType = "Command";
    }

    public class DbHelper
    {
        public static DbHelper Current;

        LiteDatabase cacheDb;

        public DbHelper()
        {
            this.Init();
        }

        private void Init()
        {
            if (!Directory.Exists("tmp"))
                Directory.CreateDirectory("tmp");
            var cacheFile = "tmp/data.db";
            if (File.Exists(cacheFile))
            {
                File.Delete(cacheFile);
            }

            cacheDb = new LiteDatabase(cacheFile);
        }

        public long Put<T>(string catelog,T value) where T : class
        {
            var collection = cacheDb.GetCollection<Db<T>>(catelog);
            var dbData = new Db<T>(value);
            var id = collection.Insert(dbData);
            return id;
        }

        public void Delete(string catelog)
        {
            if (!cacheDb.CollectionExists(catelog))
                return;
            cacheDb.DropCollection(catelog);
        }

        public void Delete(string catelog, long id)
        {
            if (!cacheDb.CollectionExists(catelog))
                return;
            var collection = cacheDb.GetCollection(catelog);
            collection.Delete(id);
        }

        public T Get<T>(string catelog, long id) where T : class
        {
            if (!cacheDb.CollectionExists(catelog))
                return default(T);
            var collection = cacheDb.GetCollection<Db<T>>(catelog);
            var data = collection.FindById(id);
            if (data == null)
                return default(T);
            else
            {
                var result = data.Value;
                collection.Delete(id);
                return result;
            }
        }

        public List<T> Get<T>(string catelog) where T : class
        {
            try
            {
                var items = GetItems<T>(catelog);
                return items.Select(x => x.Value).ToList();
            }
            catch (Exception ex)
            {
                LogHelper.Debug(ex.ToString());
                return new List<T>();
            }
        }

        private List<Db<T>> GetItems<T>(string catelog) where T : class
        {
            try
            {
                if (!cacheDb.CollectionExists(catelog))
                    return new List<Db<T>>();
                var col = cacheDb.GetCollection<Db<T>>(catelog);
                return col.FindAll().ToList();
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.ToString());
                return new List<Db<T>>();
            }
        }

    }



}
