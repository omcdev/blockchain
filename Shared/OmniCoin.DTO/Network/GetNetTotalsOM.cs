


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.DTO
{
    public class GetNetTotalsOM
    {
        public long totalBytesRecv { get; set; }
        public long totalBytesSent { get; set; }
        public long timeMillis { get; set; }
    }
}
