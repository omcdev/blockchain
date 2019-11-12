using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OmniCoin.MiningPool.API
{
    public class CommonResponse
    {
        public JToken Data { get; set; }

        public string Extension { get; set; }

        public int Code { get; set; }

        public string Message { get; set; }
    }
}
