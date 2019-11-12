using ons;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.AliMQ
{
    public class ConsumerOrderMessage<T> : ConsumerMessage<T> where T : class
    {
        private OrderConsumer orderConsumer;
        private static List<T> orderMessageList = new List<T>();
        
        public ConsumerOrderMessage()
        {
            Initialize("Reward", "CID_MinerReward_Producer");
        }

        public void InitializeOrderConsumer()
        {
            orderConsumer = ONSFactory.getInstance().createOrderConsumer(factoryInfo);
        }

        public void ReceiveOrderMessage(string tag)
        {
            orderConsumer.subscribe(factoryInfo.getPublishTopics(), tag, new MyMsgOrderListener());
            orderConsumer.start();
        }

        public void OrderMessageDispose()
        {
            orderConsumer.shutdown();
        }

        public class MyMsgOrderListener : MessageOrderListener
        {
            public MyMsgOrderListener()
            {
            }

            ~MyMsgOrderListener()
            {
            }

            public override ons.OrderAction consume(Message value, ConsumeOrderContext context)
            {
                Byte[] text = Encoding.Default.GetBytes(value.getBody());
                //这是输出
                orderMessageList.Add(Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(text)));
                return ons.OrderAction.Success;
            }
        }
    }
}
