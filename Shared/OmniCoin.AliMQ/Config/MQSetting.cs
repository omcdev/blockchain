using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.AliMQ.Config
{
    public class MQSetting
    {
        /// <summary>
        /// 
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ConsumerId { get; set; }

        public string ProducerId { get; set; }

        /// <summary>
        /// Topic
        /// </summary>
        public string PublishTopics { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ONSAddr { get; set; }
    }
}
