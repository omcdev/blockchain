using ons;
using System;
using System.Collections.Generic;
using OmniCoin.Framework;

namespace OmniCoin.AliMQ
{
    /// <summary>
    /// 发送普通消息
    /// </summary>
    public class ProducerMessage<T> where T : class
    {
        public ONSFactoryProperty factoryInfo;
        public static Action<string, T> ProducerMessageAction = PublishMessage;
        private Producer producer;

        public ProducerMessage()
        {
            factoryInfo = new ONSFactoryProperty();
        }

        public void Initialize(string topic, string producerId)
        {
            // AccessKey 阿里云身份验证，在阿里云服务器管理控制台创建
            factoryInfo.setFactoryProperty(ONSFactoryProperty.AccessKey, "AccessKey");
            // SecretKey 阿里云身份验证，在阿里云服务器管理控制台创建
            factoryInfo.setFactoryProperty(ONSFactoryProperty.SecretKey, "SecretKey");
            // 您在 MQ 控制台创建的 Producer ID
            factoryInfo.setFactoryProperty(ONSFactoryProperty.ProducerId, producerId);
            // 您在 MQ 控制台创建的 Topic
            factoryInfo.setFactoryProperty(ONSFactoryProperty.PublishTopics, topic);
            // 设置接入域名（此处以公共云生产环境为例）
            factoryInfo.setFactoryProperty(ONSFactoryProperty.ONSAddr, "ONSAddr");
            // 设置日志路径
            factoryInfo.setFactoryProperty(ONSFactoryProperty.LogPath, System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "logs-dir"));
            //
            //factoryInfo.setFactoryProperty(ONSFactoryProperty.MessageModel, ONSFactoryProperty.BROADCASTING);
        }

        public static void PublishMessage(string tag, T list)
        {
            ProducerMessage<T> producer = new ProducerMessage<T>();
            producer.Initialize("ProductionReward", "PID_ProductionReward");
            producer.InitializeNormalProducer();
            producer.SendNormalMessage(list, tag);

            producer.SendDispose();
        }

        public void SendDispose()
        {
            producer.shutdown();
        }

        public void InitializeNormalProducer()
        {
            producer = ONSFactory.getInstance().createProducer(factoryInfo);
            producer.start();
        }

        #region 发送普通消息

        /// <summary>
        /// 发送普通消息
        /// </summary>
        public void SendNormalMessage(List<T> byteBody, string tags)
        {
            try
            {
                foreach (var item in byteBody)
                {
                    Message msg = new Message(factoryInfo.getPublishTopics(), tags, Newtonsoft.Json.JsonConvert.SerializeObject(item));
                    msg.setKey(Guid.NewGuid().ToString());
                    SendResultONS sendResult = producer.send(msg);
                    LogHelper.Info($"send success {sendResult.getMessageId()}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"send failure {ex.ToString()}");
            }
        }

        public void SendNormalMessage(T byteBody, string tags)
        {
            Message msg = new Message(factoryInfo.getPublishTopics(), tags, Newtonsoft.Json.JsonConvert.SerializeObject(byteBody));
            msg.setKey(Guid.NewGuid().ToString());
            SendResultONS sendResult = producer.send(msg);
            LogHelper.Info($"send success {sendResult.getMessageId()}");
        }

        /// <summary>
        /// 发送普通消息
        /// </summary>
        /// <param name="byteBody"></param>
        /// <param name="tags"></param>
        public void SendNormalMessage(string byteBody, string tags)
        {
            try
            {
                Message msg = new Message(factoryInfo.getPublishTopics(), tags, byteBody);
                msg.setKey(Guid.NewGuid().ToString());
                SendResultONS sendResult = producer.send(msg);
                Console.WriteLine("send success {0}", sendResult.getMessageId());
            }
            catch (Exception ex)
            {
                LogHelper.Error($"send failure {ex.ToString()}");
            }
        }

        #endregion 发送普通消息
    }
}