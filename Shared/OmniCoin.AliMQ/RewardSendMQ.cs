using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.AliMQ
{
    public class RewardSendMQ
    {
        public string Address { get; set; }

        public string SN { get; set; }

        public string Account { get; set; }

        public long Reward { get; set; }

        public long CurrentDate { get; set; }
    }
}
