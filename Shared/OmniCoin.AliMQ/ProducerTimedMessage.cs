using ons;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.AliMQ
{
    /// <summary>
    /// 发送定时消息
    /// </summary>
    public class ProducerTimedMessage<T> : ProducerMessage<T> where T : class
    {
        private Producer timedProducer;

        public ProducerTimedMessage()
        {
            Initialize("PayReward", "PID_MinerReward_Producer");
        }

        public void InitializeOrderProducer()
        {
            timedProducer = ONSFactory.getInstance().createProducer(factoryInfo);
            timedProducer.start();
        }

        /// <summary>
        /// 发送定时消息
        /// </summary>
        /// <param name="tag">二级标签</param>
        /// <param name="byteBody">消息内容</param>
        /// <param name="keys">设置代表消息的业务关键属性，请尽可能全局唯一</param>
        /// <param name="deliverTime">单位 ms，指定一个时刻，在这个时刻之后才能被消费</param>
        public void SendTimedMessage(string tag, string byteBody, string keys, long deliverTime)
        {
            try
            {
                Message msg = new Message(factoryInfo.getPublishTopics(), tag, byteBody);
                msg.setKey(keys);
                msg.setStartDeliverTime(deliverTime);
                SendResultONS sendResult = timedProducer.send(msg);
            }
            catch (Exception ex)
            {
                //发送失败处理
            }
        }

        public void SendTimedMessage(string tag, List<T> byteBody, string keys, long deliverTime)
        {
            try
            {
                foreach (var item in byteBody)
                {
                    Message msg = new Message(factoryInfo.getPublishTopics(), tag, Newtonsoft.Json.JsonConvert.SerializeObject(item));
                    msg.setKey(keys);
                    msg.setStartDeliverTime(deliverTime);
                    SendResultONS sendResult = timedProducer.send(msg);
                }
            }
            catch (Exception ex)
            {
                //发送失败处理
            }
        }

        /// <summary>
        /// 销毁TimedMessage对象
        /// </summary>
        public void TimedMessageDispose()
        {
            timedProducer.shutdown();
        }
    }
}
