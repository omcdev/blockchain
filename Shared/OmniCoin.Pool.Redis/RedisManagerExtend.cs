//using CSRedis;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace OmniCoin.Pool.Redis
//{
//    public class RedisManagerExtend
//    {
//        //这种无法使用单例模式,需要改善
//        public static RedisManagerExtend _current;
//        public static RedisManagerExtend Current
//        {
//            get
//            {
//                if (_current == null)
//                {
//                    _current = new RedisManagerExtend(DefualtDatabase);
//                }
//                return _current;
//            }
//        }

//        public static int DefualtDatabase { get; set; }

//        public string connectionString = "127.0.0.1:6379,poolsize=10,ssl=false,writeBuffer=10240,prefix=key前辍";

//        CSRedisClient _redisClient = null;
//        public RedisManagerExtend(int defaultDatabase)
//        {
//            //普通模式/集群模式
//            /*
//            _redisClient = new CSRedisClient[14];
//            for (var a = 0; a < _redisClient.Length; a++)
//            {
//                _redisClient[a] = new CSRedisClient(connectionString + "; defualtDatabase=" + a);
//                //初始化
//                RedisHelper.Initialization(_redisClient[a]);
//            }
//            */
//            _redisClient = new CSRedisClient(null, connectionString + "; defualtDatabase=" + defaultDatabase);
//            RedisHelper.Initialization(_redisClient);
//        }

//        public bool SaveDataToRedis<T>(string key, T value, int defaultDatabase)
//        {
//            DefualtDatabase = defaultDatabase;
//            return RedisHelper.Set(key, value, 3600);
//        }

//        public bool SaveDataToRedis<T>(string key, T value, int defaultDatabase, int timeout)
//        {
//            DefualtDatabase = defaultDatabase;
//            return RedisHelper.Set(key, value, timeout);
//        }


//        public long RemoveDataInRedis(string key, int defaultDatabase)
//        {
//            DefualtDatabase = defaultDatabase;
//            return RedisHelper.Del(key);
//        }

//        public T GetDataInRedis<T>(string key, int defaultDatabase)
//        {
//            DefualtDatabase = defaultDatabase;
//            var result = RedisHelper.Get<T>(key);
//            return result;
//        }

//        public long PTtl(string key, int defaultDatabase)
//        {
//            DefualtDatabase = defaultDatabase;
//            return RedisHelper.PTtl(key);
//        }

        
//    }
//}
