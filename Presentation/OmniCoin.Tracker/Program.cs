

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Business;
using OmniCoin.Entities;
using OmniCoin.Framework;
using System;
using System.Threading.Tasks;

namespace OmniCoin.Tracker
{
    class Program
    {
        static void Main(string[] args)
        {
            bool testnet = false;

            if (args.Length == 1 && args[0].ToLower() == "-testnet")
            {
                testnet = true;
                LogHelper.Info("OmniCoin Tracker Server Testnet is Started.");
            }
            else
            {
                LogHelper.Info("OmniCoin Tracker Server is Started.");
            }

            try
            {
                GlobalParameters.IsTestnet = testnet;
                var p2pComponent = new P2PComponent();
                ConfigCenter.ConfigNode = new NodeConfig();
                int port = GlobalParameters.IsTestnet ? ConfigCenter.ConfigNode.TestnetTrackerPort : ConfigCenter.ConfigNode.MainnetTrackerPort;                
                p2pComponent.P2PStart(Guid.NewGuid(), "", port, true);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
            }
        }
    }
}
