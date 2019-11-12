


using LevelDB;
using System;
using System.Collections.Generic;
using System.IO;

namespace OmniCoin.Update.Db
{
    public class LevelDomain
    {
        DB _db;
        public LevelDomain(string file)
        {
            if (!Directory.Exists(file))
                Directory.CreateDirectory(file);
            var options = new Options { CreateIfMissing = true };
            _db = new DB(options, file);
        }

        public void Put<T>(string key, T value) where T : class
        {
            if (value == null)
                throw new ArgumentNullException("Value");
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(value);
            Put(key, json);
        }

        public void Put(string key, string value)
        {
            _db.Put(key, value);
        }

        public void Put(KeyValuePair<string, string>[] keyValuePairs)
        {
            if (keyValuePairs == null)
                throw new ArgumentNullException("keyValuePairs");
            using (var batch = new WriteBatch())
            {
                foreach (var item in keyValuePairs)
                {
                    _db.Put(item.Key, item.Value);
                }
                var writeOptions = new WriteOptions { Sync = true };
                _db.Write(batch, writeOptions);
            }
        }

        public void Put<T>(IEnumerable<KeyValuePair<string, T>> keyValuePairs)
        {
            if (keyValuePairs == null)
                throw new ArgumentNullException("keyValuePairs");
            using (var batch = new WriteBatch())
            {
                foreach (var item in keyValuePairs)
                {
                    _db.Put(item.Key, Newtonsoft.Json.JsonConvert.SerializeObject(item.Value));
                }
                var writeOptions = new WriteOptions { Sync = true };
                _db.Write(batch, writeOptions);
            }
        }

        public void Del(string key)
        {
            _db.Delete(key);
        }

        public void Del(IEnumerable<string> keys)
        {
            using (var batch = new WriteBatch())
            {
                foreach (var key in keys)
                {
                    _db.Delete(key);
                    var writeOptions = new WriteOptions { Sync = true };
                    _db.Write(batch, writeOptions);
                }
            }
        }

        public string Get(string key)
        {
            return _db.Get(key);
        }

        public IEnumerable<T> Get<T>(IEnumerable<string> keys)
        {
            List<T> result = new List<T>();
            using (var snapshot = _db.CreateSnapshot())
            {
                foreach (var key in keys)
                {
                    var readOptions = new ReadOptions { Snapshot = snapshot };
                    var json = _db.Get(key, readOptions);
                    if (!string.IsNullOrEmpty(json))
                    {
                        var item = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
                        if (item != null)
                            result.Add(item);
                    }
                }
            }
            return result;
        }

        public IEnumerable<string> Get(IEnumerable<string> keys)
        {
            List<string> result = new List<string>();
            using (var snapshot = _db.CreateSnapshot())
            {
                foreach (var key in keys)
                {
                    var readOptions = new ReadOptions { Snapshot = snapshot };
                    var item = _db.Get(key, readOptions);
                    result.Add(item);
                }
            }
            return result;
        }

        public T Get<T>(string key) where T : class
        {
            var json = Get(key);
            if (string.IsNullOrEmpty(json))
                return default(T);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }

        public void Close()
        {
            _db.Close();
            _db.Dispose();
        }
    }
}
