using FiiiChain.Framework;
using RdKafka;
using System;
using System.Text;
using System.Threading.Tasks;

namespace FiiiChain.KafkaMQ
{
    internal class ProducerMessage
    {
        private Topic topic;
        internal Producer Producer;

        private static ProducerMessage _instance;
        internal static ProducerMessage Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ProducerMessage();
                }
                return _instance;
            }
        }

        internal ProducerMessage()
        {
            Producer = new Producer(KafkaInfo.IP);
        }

        internal void CreateTopic(string topicName)
        {
            topic = Producer.Topic(topicName);
        }

        internal void Send(string json)
        {
            if (json == null)
                return;

            byte[] data = Encoding.Default.GetBytes(json);
            topic.Produce(data);
        }

        internal void ProducerDispose()
        {
            topic.Dispose();
            Producer.Dispose();
        }

        internal static Action<string, string> ProducerMessageAction = (topic, json) =>
        {
            try
            {
                Instance.CreateTopic(topic);
                Instance.Send(json);
            }
            catch(Exception ex)
            {
                LogHelper.Error(ex.ToString());
            }
        };
    }
}
