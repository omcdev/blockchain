using OmniCoin.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.RabbitMQ
{
    internal class ConsumerMessage
    {
        public static EventingBasicConsumer consumer = null;
        public static IModel channel = null;
        public static Action<Dictionary<string, SafeCollection<Action<string>>>> ConsumerMessageAction = (dicData) =>
        {
            List<string> topics = dicData.Select(x => x.Key).ToList();
            ConnectionFactory factory = new ConnectionFactory();
            factory.Uri = new Uri(RabbitMqSetting.CONNECTIONSTRING);

            IConnection connection = factory.CreateConnection();
            channel = connection.CreateModel();
            //channel.BasicQos(0, 1, false);
            consumer = new EventingBasicConsumer(channel);
            //定义不同的消费者消费不同的队列
            foreach (string topic in topics)
            {
                channel.BasicConsume(topic, true, consumer);
            }
            consumer.Received += (model, ea) =>
            {
                try
                {
                    var body = ea.Body;
                    string message = Encoding.UTF8.GetString(body);
                    LogHelper.Info($"MQ Received Msg [{ea.RoutingKey}]");
                    //确认该消息已被消费
                    //channel.BasicAck(ea.DeliveryTag, false);
                    if (dicData.ContainsKey(ea.RoutingKey))
                    {
                        foreach (Action<string> action in dicData[ea.RoutingKey])
                        {
                            action.Invoke(message);
                        }
                    }
                }
                catch(Exception ex)
                {
                    LogHelper.Error(ex.ToString());
                }
            };
        };
    }
}
