


using OmniCoin.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Update.Db
{
    public class UserSetting
    {
        public const string AddressBook = "AddressBook";
        public const string AccountBook = "AccountBook";
        public const string PaymentRequestBook = "PaymentRequestBook";

        public const string FeePerKB = "FeePerKB";
        public const string Encrypt = "Encrypt";
        public const string PassCiphertext = "PassCiphertext";
    }

    public class UserTables
    {
        public const string Account = "Account";
        public const string AddressItem = "AddressItem";
        public const string TxComment = "TxComment";
        public const string PaymentRequest = "PaymentRequest";
    }

    public class AccountDac : UserDbBase<AccountDac>
    {
        internal List<string> AccountBook = new List<string>();
        public static Action<string> InsertAccountEvent;

        public AccountDac()
        {
            Load();
        }

        private void Load()
        {
            AccountBook.AddRange(LoadAccountBook());
        }

        #region AccountBook
        public List<string> GetAccountBook()
        {
            return AccountBook;
        }

        internal IEnumerable<string> LoadAccountBook()
        {
            return UserDomain.Get<IEnumerable<string>>(UserSetting.AccountBook) ?? new List<string>();
        }

        internal void UpdateAccountBook(IEnumerable<string> accountBook)
        {
            UserDomain.Put(UserSetting.AccountBook, accountBook);
        }
        #endregion

        #region Update
        public virtual void Insert(Account account)
        {
            if (account == null)
                throw new ArgumentNullException("Account");
            var key = GetKey(UserTables.Account, account.Id);
            this.UserDomain.Put(key, account);

            AccountBook.Add(account.Id);
            UpdateAccountBook(AccountBook);
        }

        public virtual void Insert(IEnumerable<Account> accounts)
        {
            if (accounts == null)
                throw new ArgumentNullException("Account");
            Dictionary<string, Account> pairs = new Dictionary<string, Account>();
            foreach (var account in accounts)
            {
                var key = GetKey(UserTables.Account, account.Id);
                pairs.Add(key, account);
            }
            this.UserDomain.Put(pairs);

            AccountBook.AddRange(accounts.Select(x => x.Id));
            UpdateAccountBook(AccountBook);
        }

        public virtual void UpdateBalance(string id, long amount)
        {
            var key = GetKey(UserTables.Account, id);
            var account = UserDomain.Get<Account>(key);
            if (account == null)
                return;

            account.Balance = amount;
            UserDomain.Put(key, account);
        }

        public virtual void UpdateTag(string id, string tag)
        {
            var key = GetKey(UserTables.Account, id);
            var account = UserDomain.Get<Account>(key);
            if (account == null)
                return;

            account.Tag = tag;
            UserDomain.Put(key, account);
        }

        public virtual int UpdatePrivateKeyAr(IEnumerable<Account> aclist)
        {
            if (aclist == null || !aclist.Any())
                return 0;

            int result = 0;
            Dictionary<string, Account> pairs = new Dictionary<string, Account>();
            foreach (var ac in aclist)
            {
                var key = GetKey(UserTables.Account, ac.Id);
                var account = UserDomain.Get<Account>(key);
                if (account == null)
                    continue;
                account.PrivateKey = ac.PrivateKey;
                pairs.Add(key, account);
                result++;
            }
            UserDomain.Put<Account>(pairs);
            return result;
        }
        #endregion

        #region Delete
        public virtual void Delete(string id)
        {
            var key = GetKey(UserTables.Account, id);
            UserDomain.Del(key);

            AccountBook.Remove(id);
            UpdateAccountBook(AccountBook);
        }
        #endregion

        #region Query
        public virtual IEnumerable<Account> SelectAll()
        {
            var keys = AccountBook.Select(x => GetKey(UserTables.Account, x));
            var result = UserDomain.Get<Account>(keys);
            return result;
        }

        public virtual Account SelectById(string id)
        {
            var key = GetKey(UserTables.Account, id);
            var account = UserDomain.Get<Account>(key);
            return account;
        }

        public virtual IEnumerable<Account> SelectByTag(string tag)
        {
            var accounts = SelectAll();
            if (accounts == null || !accounts.Any())
                return null;
            return accounts.Where(x => x.Tag == tag);
        }

        public virtual bool IsExisted(string id)
        {
            return AccountBook.Contains(id);
        }
        #endregion
    }
}
