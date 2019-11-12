


using OmniCoin.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Data.Dacs
{
    public class SettingDac : UserDbBase<SettingDac>
    {
        public Setting Setting = null;

        public SettingDac()
        {
            Setting = GetAppSetting();
        }

        #region Setting
        public Setting GetAppSetting()
        {
            Setting setting = new Setting();
            var feePerKB = UserDomain.Get(AppSetting.FeePerKB);
            var passCiphertext = UserDomain.Get(AppSetting.PassCiphertext);
            var encrypt = UserDomain.Get(AppSetting.Encrypt);

            setting.Confirmations = 1;
            long fee;
            if (feePerKB != null && long.TryParse(feePerKB, out fee))
            {
                setting.FeePerKB = fee;
            }
            else
            {
                setting.FeePerKB = 100000;
            }

            bool isEncrypt;
            if (encrypt != null && bool.TryParse(encrypt, out isEncrypt))
            {
                setting.Encrypt = isEncrypt;
            }
            else
            {
                setting.Encrypt = false;
            }

            setting.PassCiphertext = passCiphertext;
            return setting;
        }

        public void SetAppSetting(Setting setting)
        {
            Setting = setting;
            UserDomain.Put(AppSetting.Encrypt, setting.Encrypt.ToString());
            UserDomain.Put(AppSetting.FeePerKB, setting.FeePerKB.ToString());
            if (!string.IsNullOrEmpty(setting.PassCiphertext))
                UserDomain.Put(AppSetting.PassCiphertext, setting.PassCiphertext);
        }
        #endregion
    }
}