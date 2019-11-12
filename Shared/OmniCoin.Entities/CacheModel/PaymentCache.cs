


using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities.CacheModel
{
    public static class PaymentCatelog
    {
        public const string Send = "send";
        public const string Generate = "generate";
        public const string Self = "self";
        public const string Receive = "receive";
    }

    public class PaymentCache
    {
        public string txId { get; set; }
        public int vout { get; set; }
        public string account { get; set; }
        public string address { get; set; }
        public string category { get; set; }
        public long amount { get; set; }
        public long totalInput { get; set; }
        public long totalOutput { get; set; }
        public long fee { get; set; }
        public long time { get; set; }
        public int size { get; set; }
        public string comment { get; set; }
        public long confirmations { get; set; }
        public string blockHash { get; set; }
        public int blockIndex { get; set; }
        public long blockTime { get; set; }

        public override string ToString()
        {
            var key = $"{category}_{address}_{amount}_{time}_{txId}_{vout}";
            return key;
        }

        public static bool IsSameTrsaction(PaymentCache current, PaymentCache paymentCache)
        {
            if (current == null)
                return true;
            return current.txId == paymentCache.txId && current.vout == paymentCache.vout;
        }
    }
}