


using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniCoin.AliMQ
{
    public class MqManager
    {
        private static MqManager _mqManager;

        public static MqManager Current
        {
            get
            {
                if (_mqManager == null)
                    _mqManager = new MqManager();
                return _mqManager;
            }
        }

        Dictionary<string, SafeCollection<Action<object>>> msgs = new Dictionary<string, SafeCollection<Action<object>>>();

        public void Regist(string key, Action<object> action)
        {
            if (msgs.ContainsKey(key))
            {
                msgs[key].Add(action);
            }
            else
            {
                SafeCollection<Action<object>> actions = new SafeCollection<Action<object>>();
                actions.Add(action);
                msgs.Add(key, actions);
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Send<T>(string key, T value) where T : class
        {
            ProducerMessage<T>.ProducerMessageAction(key, value);
        }

        /// <summary>
        /// 监听消息
        /// </summary>
        public void Listen()
        {
            var flag = true;
            while (flag)
            {
                var keys = msgs.Select(x=>x.Key);

                Parallel.ForEach(keys, x => StartTask(x));
            }
        }

        void StartTask(string tag)
        {
            var obj = ConsumerMessage<object>.ConsumerMessageFunc(tag);

            foreach (var item in obj)
            {
                foreach (Action<object> action in msgs[tag])
                {
                    action.Invoke(item);
                }
            }
        }
    }
}
