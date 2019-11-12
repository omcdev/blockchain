


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.DTO
{
    public class SendManyOutputIM
    {
        public string address { get; set; }
        public string tag { get; set; }
        public long amount { get; set; }
        public string comment { get; set; }
    }
}
