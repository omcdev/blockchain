using ons;
using System;
using System.Collections.Generic;

namespace OmniCoin.AliMQ
{
    /// <summary>
    /// 发送顺序消息
    /// </summary>
    public class ProducerOrderMessage<T> : ProducerMessage<T> where T : class
    {
        private OrderProducer orderProducer;

        public ProducerOrderMessage()
        {
            Initialize("PayReward", "PID_MinerReward_Producer");
        }

        public void InitializeOrderProducer()
        {
            orderProducer = ONSFactory.getInstance().createOrderProducer(factoryInfo);
            orderProducer.start();
        }

        public void SendOrderMessage(string tag, string byteBody, string shardingKey)
        {
            try
            {
                Message msg = new Message(factoryInfo.getPublishTopics(), tag, byteBody);
                SendResultONS sendResult = orderProducer.send(msg, shardingKey);
                Console.WriteLine("send success {0}", sendResult.getMessageId());
            }
            catch (Exception ex)
            {
                Console.WriteLine("send failure{0}", ex.ToString());
            }
        }

        public void SendOrderMessage(string tag, List<T> byteBody, string shardingKey)
        {
            try
            {
                foreach (var item in byteBody)
                {
                    Message msg = new Message(factoryInfo.getPublishTopics(), tag, Newtonsoft.Json.JsonConvert.SerializeObject(item));
                    SendResultONS sendResult = orderProducer.send(msg, shardingKey);
                    Console.WriteLine("send success {0}", sendResult.getMessageId());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("send failure{0}", ex.ToString());
            }
        }

        public void OrderMessageDispose()
        {
            orderProducer.shutdown();
        }
    }
}
