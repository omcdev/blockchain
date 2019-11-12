using System;
using System.Collections.Generic;
using System.Text;

namespace MultiThreadingMiners.Api
{
    public class ApiResponseData
    {
        public object Data { get; set; }

        public string Extension { get; set; }

        public int Code { get; set; }

        public string Message { get; set; }
    }
}
