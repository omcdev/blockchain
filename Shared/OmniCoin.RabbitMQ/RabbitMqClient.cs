using OmniCoin.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.RabbitMQ
{
    public class RabbitMqClient
    {
        private static RabbitMqClient _client;

        public static RabbitMqClient Current
        {
            get
            {
                if (_client == null)
                {
                    _client = new RabbitMqClient();
                }
                return _client;
            }
        }
        public static void ProduceMessage(List<string> messages)
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.Uri = new Uri(RabbitMqSetting.CONNECTIONSTRING);
            /*
            factory.HostName = RabbitMqSetting.HOSTNAME;
            factory.UserName = RabbitMqSetting.USERNAME;
            factory.Password = RabbitMqSetting.PASSWORD;
            */
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    //创建一个名称为hello的消息队列
                    //是否持久化
                    bool durable = true;
                    channel.QueueDeclare(RabbitMqName.Default, durable, false, false, null);
                    if (messages != null && messages.Count > 0)
                    {
                        foreach (string message in messages)
                        {
                            var properties = channel.CreateBasicProperties();
                            properties.Persistent = true;

                            byte[] body = Encoding.UTF8.GetBytes(message);
                            channel.BasicPublish("", RabbitMqName.Default, properties, body);
                            LogHelper.Info($"send message：{message}");
                        }
                    }
                }
            }
        }

        public static void ProduceMessage(string message)
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.Uri = new Uri(RabbitMqSetting.CONNECTIONSTRING);

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    //创建一个名称为hello的消息队列
                    //是否持久化
                    bool durable = true;
                    channel.QueueDeclare(RabbitMqName.Default, durable, false, false, null);
                    if (!string.IsNullOrEmpty(message))
                    {
                        var properties = channel.CreateBasicProperties();
                        properties.Persistent = true;

                        byte[] body = Encoding.UTF8.GetBytes(message);
                        channel.BasicPublish(ExchangeType.Direct, RabbitMqName.Default, properties, body);
                        LogHelper.Info($"send message：{message}");
                    }
                }
            }
        }

        public void ProduceMessage(string queue, string routeKey, string message)
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.Uri = new Uri(RabbitMqSetting.CONNECTIONSTRING);

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    //定义一个Direct类型交换机
                    channel.ExchangeDeclare(RabbitMqSetting.EXCHANGENAME, ExchangeType.Direct, true, false, null);
                    //定义队列
                    channel.QueueDeclare(queue, true, false, false, null);
                    //将队列绑定到交换机
                    channel.QueueBind(queue, RabbitMqSetting.EXCHANGENAME, routeKey, null);
                    if (!string.IsNullOrEmpty(message))
                    {
                        byte[] body = Encoding.UTF8.GetBytes(message);
                        channel.BasicPublish(RabbitMqSetting.EXCHANGENAME, routeKey, null, body);
                        LogHelper.Info($"send message [{routeKey}]");
                    }
                }
            }
        }

        /// <summary>
        /// 矿池奖励专用方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messages"></param>
        public static void ProduceMessage<T>(List<T> messages) where T : class
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.Uri = new Uri(RabbitMqSetting.CONNECTIONSTRING);

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    //创建一个名称为hello的消息队列
                    //是否持久化
                    bool durable = true;
                    channel.QueueDeclare(RabbitMqName.PosInviteReward, durable, false, false, null);
                    if (messages != null && messages.Count > 0)
                    {
                        foreach (T message in messages)
                        {
                            var properties = channel.CreateBasicProperties();
                            properties.Persistent = true;

                            byte[] body = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(message));
                            channel.BasicPublish("", RabbitMqName.PosInviteReward, properties, body);
                            LogHelper.Info($"send message：{Newtonsoft.Json.JsonConvert.SerializeObject(message)}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 接收消息
        /// 这个只是一个例子，实际使用中IConnection和IModel确定没有消息了才可以释放
        /// </summary>
        public static void ConsumMessage()
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.Uri = new Uri(RabbitMqSetting.CONNECTIONSTRING);

            using (IConnection connection = factory.CreateConnection())
            {
                using (IModel channel = connection.CreateModel())
                {

                    bool durable = true;
                    channel.QueueDeclare(RabbitMqName.Default, durable, false, false, null);
                    //公平分发
                    channel.BasicQos(0, 1, false);

                    var consumer = new EventingBasicConsumer(channel);
                    channel.BasicConsume(RabbitMqName.Default, false, consumer);

                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body;
                        var message = Encoding.UTF8.GetString(body);
                        LogHelper.Info($"receive message：{message}");
                        //确认该消息已被消费
                        channel.BasicAck(ea.DeliveryTag, false);
                    };
                }
            }
        }

        /// <summary>
        /// 响应方主动拉取消息
        /// </summary>
        public static void PullMessage()
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.Uri = new Uri(RabbitMqSetting.CONNECTIONSTRING);

            using (IConnection connection = factory.CreateConnection())
            {
                using (IModel channel = connection.CreateModel())
                {
                    while (true)
                    {
                        BasicGetResult res = channel.BasicGet(RabbitMqName.Default, true);
                        if (res != null)
                        {
                            Console.WriteLine("receiver:" + UTF8Encoding.UTF8.GetString(res.Body));
                        }
                        else
                        {
                            return;
                        }
                    }
                }
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

        /// <summary>
        /// 监听消息
        /// </summary>
        public void Listen()
        {
            ConsumerMessage.ConsumerMessageAction(msgs);
        }
    }
}
