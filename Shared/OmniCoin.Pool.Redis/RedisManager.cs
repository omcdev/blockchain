using CSRedis;
using System;

namespace OmniCoin.Pool.Redis
{
    public class RedisManager
    {
        public static RedisManager _current;
        public static RedisManager Current
        {
            get
            {
                if (_current == null)
                    _current = new RedisManager();
                return _current;
            }
        }

        CSRedisClient _redisClient = null;
        public RedisManager()
        {
            //普通模式/集群模式
            _redisClient = new CSRedisClient(null, Setting.RedisConnections);
            //初始化
            RedisHelper.Initialization(_redisClient);
        }

        public bool SaveDataToRedis<T>(string key, T value)
        {
            return RedisHelper.Set(key, value, 3600);
        }

        public bool SaveDataToRedis<T>(string key, T value,int timeout)
        {
            return RedisHelper.Set(key, value, timeout);
        }


        public long RemoveDataInRedis(string key)
        {
            return RedisHelper.Del(key);
        }

        public T GetDataInRedis<T>(string key)
        {
            var result = RedisHelper.Get<T>(key);
            return result;
        }

        public long PTtl(string key)
        {
            return RedisHelper.PTtl(key);
        }
    }
}
