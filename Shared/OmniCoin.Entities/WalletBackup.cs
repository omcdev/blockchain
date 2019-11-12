


using OmniCoin.Entities.CacheModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities
{
    public class WalletBackup
    {
        public List<Account> AccountList { get; set; }

        public List<AddressBookItem> AddressBookItemList { get; set; }

        public List<Setting> SettingList { get; set; }

        public List<TransactionComment> TransactionCommentList { get; set; }
    }

    public class WalletBackup1
    {
        public List<AccountCache> AccountList { get; set; }

        public List<AddressBookItemCache> AddressBookItemList { get; set; }

        public List<Setting> SettingList { get; set; }

        public List<TransactionComment> TransactionCommentList { get; set; }

        public WalletBackup ToBackup()
        {
            WalletBackup backup = new WalletBackup();
            if (this.AccountList != null)
            {
                backup.AccountList = new List<Account>();
                this.AccountList.ForEach(x =>
                {
                    backup.AccountList.Add(new Account()
                    {
                        Balance = x.Balance,
                        Id = x.Address,
                        IsDefault = x.IsDefault,
                        PrivateKey = x.PrivateKey,
                        PublicKey = x.PublicKey,
                        Tag = x.Tag,
                        Timestamp = x.Timestamp,
                        WatchedOnly = x.WatchedOnly
                    });
                });
            }
            if (this.AddressBookItemList != null)
            {
                backup.AddressBookItemList = new List<AddressBookItem>();
                this.AddressBookItemList.ForEach(x =>
                {
                    backup.AddressBookItemList.Add(new AddressBookItem()
                    {
                        Address = x.Address,
                        Timestamp = x.Timestamp,
                        Tag = x.Tag,
                        Id = x.AddressId
                    });
                });
            }
            backup.SettingList = this.SettingList;
            backup.TransactionCommentList = this.TransactionCommentList;
            return backup;
        }
    }
}
