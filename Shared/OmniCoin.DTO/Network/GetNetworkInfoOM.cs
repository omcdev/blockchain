


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.DTO
{
    public class GetNetworkInfoOM
    {
        public int version { get; set; }
        public int minimumSupportedVersion { get; set; }
        public int protocolVersion { get; set; }
        public bool isRunning { get; set; }
        public int connections { get; set; }

    }
}
