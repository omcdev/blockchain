using OmniCoin.Framework;
using ons;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace OmniCoin.AliMQ
{
    public class ConsumerMessage<T> where T : class
    {
        public ONSFactoryProperty factoryInfo;
        private PushConsumer consumer;
        public static List<T> messageList = new List<T>();
        public static Func<string, List<T>> ConsumerMessageFunc = GetConsumerMessage;
        //public static Action<string> ConsumerMessageAction = GetConsumerMessage;

        private static List<T> GetConsumerMessage(string tag)
        {
            messageList.Clear();
            ConsumerMessage<T> message = new ConsumerMessage<T>();
            message.Initialize("ProductionReward", "CID_ProductionReward");
            message.InitializeNormalConsumer();
            message.ReceiveNormalMessage(tag);
            Thread.Sleep(30000);
            
            if (messageList != null && messageList.Count > 0)
            {
                message.ConsumerDispose();
                return messageList;
            }
            else
            {
                message.ConsumerDispose();
                return null;
            }

        }

        public ConsumerMessage()
        {
            factoryInfo = new ONSFactoryProperty();
        }
        public void Initialize(string topic, string consumerId)
        {
            // Set access key
            factoryInfo.setFactoryProperty(ONSFactoryProperty.AccessKey, "LTAI4y1K0vsy1d2j");
            // Set access secret
            factoryInfo.setFactoryProperty(ONSFactoryProperty.SecretKey, "vjmgQbLwZbKCdbhjtlYgrfOhaAl9rA");
            // Set PID
            factoryInfo.setFactoryProperty(ONSFactoryProperty.ConsumerId, consumerId);
            // Set topic
            factoryInfo.setFactoryProperty(ONSFactoryProperty.PublishTopics, topic);
            // Set access point according to your region
            factoryInfo.setFactoryProperty(ONSFactoryProperty.ONSAddr, "http://onsaddr-internet.aliyun.com/rocketmq/nsaddr4client-internet");
            // Set log path
            factoryInfo.setFactoryProperty(ONSFactoryProperty.LogPath, System.IO.Path.Combine(Environment.CurrentDirectory, "mqlog"));
            // 广播订阅方式设置
            factoryInfo.setFactoryProperty(ONSFactoryProperty.MessageModel, ONSFactoryProperty.BROADCASTING);
        }

        public void InitializeNormalConsumer()
        {
            consumer = ONSFactory.getInstance().createPushConsumer(factoryInfo);
        }

        /// <summary>
        /// 
        /// </summary>
        public void ReceiveNormalMessage(string tag)
        {
            consumer.subscribe(factoryInfo.getPublishTopics(), tag, new MyMsgListener());
            consumer.start();
        }

        public void ConsumerDispose()
        {
            consumer.shutdown();
        }

        public class MyMsgListener : MessageListener
        {
            public MyMsgListener()
            {
            }

            ~MyMsgListener()
            {
            }

            public override ons.Action consume(Message value, ConsumeContext context)
            {
                Byte[] text = Encoding.Default.GetBytes(value.getBody());
                //这里是输出
                messageList.Add(Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(text)));
                return ons.Action.CommitMessage;
            }
        }
    }
}
