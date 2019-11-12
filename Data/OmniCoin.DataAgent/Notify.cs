

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using OmniCoin.Framework;


namespace OmniCoin.DataAgent
{
    public class Notify
    {
        public static Notify Current;
        public string CallbackApp { get; set; }

        static Notify()
        {
            Current = new Notify();
        }

        public void NewTxReceived(string txHash)
        {
            if (string.IsNullOrWhiteSpace(CallbackApp) || string.IsNullOrWhiteSpace(txHash))
            {
                return;
            }
            ProcessStartInfo info = new ProcessStartInfo(CallbackApp.Split(' ')[0]);
            if (!string.IsNullOrEmpty(CallbackApp.Split(' ')[1]) && CallbackApp.Split(' ')[1].Contains("%s"))
            {
                info.Arguments = CallbackApp.Split(' ')[1].Replace("%s", txHash);
            }
            try
            {
                info.WindowStyle = ProcessWindowStyle.Hidden;
                Process.Start(info);
            }
            catch(Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
            }
        }
    }
}
