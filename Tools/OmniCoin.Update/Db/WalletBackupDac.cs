


using OmniCoin.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Update.Db
{
    public class WalletBackupDac : UserDbBase<WalletBackupDac>
    {
        public virtual int Restore(WalletBackup entity)
        {
            try
            {
                AccountDac.Default.Insert(entity.AccountList);
                AddressBookDac.Default.InsertOrUpdate(entity.AddressBookItemList);
                SettingDac.Default.SetAppSetting(entity.SettingList.FirstOrDefault());
                return 0;
            }
            catch
            {
                return -1;
            }
        }
    }
}
