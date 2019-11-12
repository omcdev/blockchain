

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Messages;
using OmniCoin.Entities;
using OmniCoin.DataAgent;
using System;
using System.Collections.Generic;
using System.Text;
using OmniCoin.Data;
using OmniCoin.Framework;
using OmniCoin.Consensus;
using System.Linq;
using OmniCoin.Data.Dacs;

namespace OmniCoin.Business
{
    public class SettingComponent
    {
        public void SaveSetting(Setting setting)
        {
            SettingDac.Default.SetAppSetting(setting);
        }

        public Setting GetSetting()
        {
            var setting = SettingDac.Default.GetAppSetting();

            if(setting == null)
            {
                setting = new Setting();
                setting.Confirmations = 1;
                setting.FeePerKB = 100000;
            }

            return setting;
        }
    }
}
