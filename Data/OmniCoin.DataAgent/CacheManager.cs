// Copyright (c) 2018 FiiiLab Technology Ltd
// Distributed under the MIT software license, see the accompanying
// file LICENSE or or http://www.opensource.org/licenses/mit-license.php.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FiiiChain.DataAgent
{
    public class CacheManager
    {
        private static CacheManager _default;
        public static CacheManager Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new CacheManager();
                }
                return _default;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="catelog"></param>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <param name="isMissInsert">为true时，如果不存在则添加，存在则不添加</param>
        public void Put<T>(string catelog, string key, T obj, bool isMissInsert = true) where T :class
        {
            CacheAccess.Default.Put(catelog, key, obj);
        }

        public void Put<T>(string catelog, IEnumerable<KeyValuePair<string, T>> keyValues, bool isMissInsert = true) where T : class
        {
            if (keyValues == null)
                return;

            var dataKvPairs = keyValues as KeyValuePair<string, T>[] ?? keyValues.ToArray();
            foreach (var item in dataKvPairs)
            {
                CacheAccess.Default.Put(catelog, item.Key, item.Value);
            }

            var keys = dataKvPairs.Select(x => x.Key);
        }

        public void DeleteAll(string catelog)
        {
            CacheAccess.Default.Del(catelog);
        }

        public void DeleteByKey(string catelog, string key)
        {
            CacheAccess.Default.Del(catelog, key);
        }

        public void DeleteByKeys(string catelog, IEnumerable<string> keys)
        {
            CacheAccess.Default.Del(catelog, keys);
        }

        public List<T> Get<T>(string catelog, Func<string, bool> filterKey = null) where T : class
        {
            var allKeys = CacheAccess.Default.GetCatelogKeys(catelog);
            if (allKeys == null || !allKeys.Any())
                return new List<T>();

            ConcurrentQueue<string> keys = new ConcurrentQueue<string>();

            if (filterKey != null)
            {
                allKeys.ForEach(key =>
                {
                    if (filterKey(key))
                        keys.Enqueue(key);
                });
                //keys = allKeys.Where(x => filterKey(x));
            }
            else
            {
                keys = new ConcurrentQueue<string>(allKeys);
            }
            
            var result = CacheAccess.Default.Get<T>(catelog, keys);
            return result;
        }

        public T Get<T>(string catelog,string key) where T : class
        {
            var result = CacheAccess.Default.Get<T>(catelog, key);
            return result;
        }

        public List<string> GetAllKeys(string catelog)
        {
            var allKeys = CacheAccess.Default.GetCatelogKeys(catelog);
            return allKeys;
        }

        public IEnumerable<string> GetAllKeys(string catelog, Predicate<string> predicate)
        {
            var allKeys = CacheAccess.Default.GetCatelogKeys(catelog, predicate);
            return allKeys;
        }

    }
}