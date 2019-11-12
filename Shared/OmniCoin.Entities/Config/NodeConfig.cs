

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities
{
    public class NodeConfig
    {
        

        public string User { get; set; }

        public string Password { get; set; }

        public bool IsPool { get; set; }

        public string WalletNotify { get; set; }

        public string LocalIP { get; set; }        

        int _P2PPortTestNet = 58802;
        public int P2PPortTestNet
        {
            get { return _P2PPortTestNet; }
            set { _P2PPortTestNet = value; }
        }
        int _P2PPortMainNet = 58801;
        public int P2PPortMainNet
        {
            get { return _P2PPortMainNet; }
            set { _P2PPortMainNet = value; }
        }

        int _RpcPortTestNet = 58804;
        public int RpcPortTestNet
        {
            get { return _RpcPortTestNet; }
            set { _RpcPortTestNet = value; }
        }

        int _RpcPortMainNet = 58803;
        public int RpcPortMainNet
        {
            get { return _RpcPortMainNet; }
            set { _RpcPortMainNet = value; }
        }


        //public string TestnetTrackerServer = "127.0.0.1";        
        //public string TestnetTrackerServer = "192.168.31.56";
        public string TestnetTrackerServer = "47.56.3.169";

        public string MainnetTrackerServer = "47.244.105.187";

        public int TestnetTrackerPort = 58806;

        public int MainnetTrackerPort = 58805;
        

    }
}