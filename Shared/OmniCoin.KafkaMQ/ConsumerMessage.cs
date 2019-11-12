using FiiiChain.Framework;
using RdKafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiiiChain.KafkaMQ
{
    internal class ConsumerMessage
    {
        private static EventConsumer consumer = null;

        internal static Action<Dictionary<string, SafeCollection<Action<string>>>> ConsumerMessageAction = (dicData) =>
        {
            List<string> topics = dicData.Select(x => x.Key).ToList();
            //配置消费者组
            Config config = new Config() { GroupId = KafkaInfo.MqName };
            LogHelper.Info(KafkaInfo.MqName);
            consumer = new EventConsumer(config, KafkaInfo.IP);

            consumer.OnMessage += (obj, msg) =>
            {
                var bytes = msg.Payload;
                if (bytes == null || bytes.Length == 0)
                    return;
                var text = Encoding.Default.GetString(bytes);
                if (string.IsNullOrEmpty(text))
                    return;

                LogHelper.Info($"MQ Received Msg [{msg.Topic}]");
                foreach (Action<string> action in dicData[msg.Topic])
                {
                    action.Invoke(text);
                }
            };
            //订阅一个或者多个Topic
            consumer.Subscribe(topics);
            //启动
            consumer.Start();
        };
    }
}
