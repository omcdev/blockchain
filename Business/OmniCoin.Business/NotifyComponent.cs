

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.DataAgent;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Business
{
    public class NotifyComponent
    {
        public void SetCallbackApp(string appFilePath)
        {
            Notify.Current.CallbackApp = appFilePath;
        }

        public void ProcessNewTxReceived(string txHash)
        {
            Notify.Current.NewTxReceived(txHash);
        }
    }
}
