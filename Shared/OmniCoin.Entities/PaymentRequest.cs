


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities
{
    public class PaymentRequest
    {
        public long Id { get; set; }
        public string AccountId { get; set; }
        public string Tag { get; set; }
        public string Comment { get; set; }
        public long Amount { get; set; }
        public long Timestamp { get; set; }
    }
}
