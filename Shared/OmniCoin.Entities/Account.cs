


using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities
{
    /// <summary>
    /// 账户表， Account是由公钥和私钥生成的
    /// </summary>
    public class Account
    {
        public string Id { get; set; }

        public string PrivateKey { get; set; }

        public string PublicKey { get; set; }

        public long Balance { get; set; }

        public bool IsDefault { get; set; }

        public bool WatchedOnly { get; set; }

        public long Timestamp { get; set; }

        public string Tag { get; set; }

        public override string ToString()
        {
            return string.Format("{0}", Id);
        }
    }
}
