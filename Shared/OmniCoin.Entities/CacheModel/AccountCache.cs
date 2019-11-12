


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities.CacheModel
{
    public class AccountCache
    {
        public int Id { get; set; }

        public string Address { get; set; }

        public string PrivateKey { get; set; }

        public string PublicKey { get; set; }

        public long Balance { get; set; }

        public bool IsDefault { get; set; }

        public bool WatchedOnly { get; set; }

        public long Timestamp { get; set; }

        public string Tag { get; set; }

        public override string ToString()
        {
            return string.Format("{0}_{1}", Address, Tag);
        }
    }
}
