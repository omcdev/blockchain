using FiiiChain.Framework;
using System;
using System.Collections.Generic;

namespace FiiiChain.KafkaMQ
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

        Dictionary<string, SafeCollection<Action<string>>> msgs = new Dictionary<string, SafeCollection<Action<string>>>();

        public void Regist(string key, Action<string> action)
        {
            if (msgs.ContainsKey(key))
            {
                msgs[key].Add(action);
            }
            else
            {
                SafeCollection<Action<string>> actions = new SafeCollection<Action<string>>();
                actions.Add(action);
                msgs.Add(key, actions);
            }
        }

        public void Send(string topic, string json)
        {
            LogHelper.Info("MQ Send Msg [" + topic + "]");
            ProducerMessage.ProducerMessageAction(topic, json);
        }

        /// <summary>
        /// 监听消息
        /// </summary>
        public void Listen()
        {
            ConsumerMessage.ConsumerMessageAction(msgs);
        }
    }
}
