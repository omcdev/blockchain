


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.ShareModels.Models
{
    public class PoolWorkingInfo
    {
        public long HashRates { get; set; }
        public long PushTime { get; set; }
        public string[] Miners { get; set; }
    }
}