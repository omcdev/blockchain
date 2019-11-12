

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Consensus;
using OmniCoin.Data.Dacs;
using OmniCoin.Data;
using OmniCoin.Entities;
using OmniCoin.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OmniCoin.Business.Extensions;
using OmniCoin.Entities.Explorer;
using System.Threading.Tasks;
using System.Threading;

namespace OmniCoin.Business
{
    public class AccountComponent
    {
        const string encryptExtensionName = ".fcx";
        const string noEncryptExtensionName = ".fct";
        public Account GenerateNewAccount(bool isAddtoCache = true)
        {
            var dac = AccountDac.Default;

            byte[] privateKey;
            byte[] publicKey;
            using (var dsa = ECDsa.GenerateNewKeyPair())
            {
                privateKey = dsa.PrivateKey;
                publicKey = dsa.PublicKey;
            }

            var id = AccountIdHelper.CreateAccountAddress(publicKey);
            if (dac.IsExisted(id))
            {
                throw new Exception("Account id is existed");
            }

            Account account = new Account();
            account.Id = id;
            account.PrivateKey = Base16.Encode(privateKey);
            account.PublicKey = Base16.Encode(publicKey);
            account.Balance = 0;
            account.IsDefault = false;
            account.WatchedOnly = false;
            
            AccountDac.Default.Insert(account);
            return account;
        }

        public long GetCondfirmedBalance(string address)
        {
            var lastHeight = GlobalParameters.LocalHeight;
            var utxosets = UtxoSetDac.Default.GetByAccounts(new string[] { address });
            var balance = utxosets.Any() ? 0 : utxosets.Where(x => x.IsSpentable(lastHeight, Time.EpochTime)).Sum(x => x.Amount);
            return balance;
        }

        public Account ImportAccount(string privateKeyText)
        {
            var dac = AccountDac.Default;

            byte[] privateKey = Base16.Decode(privateKeyText);
            byte[] publicKey;
            using (var dsa = ECDsa.ImportPrivateKey(privateKey))
            {
                publicKey = dsa.PublicKey;
            }

            var id = AccountIdHelper.CreateAccountAddress(publicKey);
            Account account = dac.SelectById(id);

            if (account == null)
            {
                account = new Account();
                account.Id = AccountIdHelper.CreateAccountAddress(publicKey);
                account.PrivateKey = Base16.Encode(privateKey);
                account.PublicKey = Base16.Encode(publicKey);
                account.Balance = 0;
                account.IsDefault = false;
                account.WatchedOnly = false;

                AccountDac.Default.Insert(account);
            }

            return account;
        }

        public Account ImportObservedAccount(string publicKeyText)
        {
            var dac = AccountDac.Default;

            var publicKey = Base16.Decode(publicKeyText);
            var id = AccountIdHelper.CreateAccountAddress(publicKey);

            Account account = dac.SelectById(id);

            if (account == null)
            {
                account = new Account();
                account.Id = AccountIdHelper.CreateAccountAddress(publicKey);
                account.PrivateKey = "";
                account.PublicKey = Base16.Encode(publicKey);
                account.Balance = 0;
                account.IsDefault = false;
                account.WatchedOnly = true;

                AccountDac.Default.Insert(account);
            }

            return account;
        }

        public Account GenerateWatchOnlyAddress(string publickeyText)
        {
            var dac = AccountDac.Default;
            byte[] publickey = Base16.Decode(publickeyText);
            string id = AccountIdHelper.CreateAccountAddress(publickey);

            Account account = dac.SelectById(id);

            if (account == null)
            {
                account = new Account();
                account.Id = id;
                account.PrivateKey = "";
                account.PublicKey = publickeyText;
                account.Balance = 0;
                account.IsDefault = false;
                account.WatchedOnly = true;

                AccountDac.Default.Insert(account);
                //UtxoSet.Instance.AddAccountId(account.Id);
            }

            return account;
        }

        public List<Account> GetAllAccountsInDb()
        {
            return AccountDac.Default.SelectAll()?.ToList();
        }

        public List<Account> GetAllAccounts()
        {
            return AccountDac.Default.SelectAll()?.ToList();
        }

        public Account GetAccountById(string id)
        {
            var dac = AccountDac.Default;
            return dac.SelectById(id);
        }

        public List<Account> GetAccountCategory(int category)
        {
            return AccountDac.Default.SelectAll()?.ToList();
        }

        public List<Account> GetAccountsByTag(string tag)
        {
            return AccountDac.Default.SelectByTag(tag)?.ToList();
        }

        public Account GetDefaultAccount()
        {
            var defaultAddr = AppDac.Default.GetDefaultAccount();
            if (string.IsNullOrEmpty(defaultAddr))
                defaultAddr = AccountDac.Default.GetAccountBook().FirstOrDefault();

            return AccountDac.Default.SelectById(defaultAddr);
        }

        public void SetDefaultAccount(string id)
        {
            var dac = AccountDac.Default;
            dac.SetDefaultAccount(id);
        }

        public void UpdateBalance(string id, long amount)
        {
            AccountDac.Default.UpdateBalance(id, amount);
        }

        public void UpdatePrivateKeyAr(Account account)
        {
            AccountDac.Default.UpdatePrivateKeyAr(new List<Account>(new Account[]{ account }));
        }

        public void DeleteAccount(string id)
        {
            AccountDac.Default.Delete(id);
        }

        public void UpdateTag(string id, string tag)
        {
            AccountDac.Default.UpdateTag(id, tag);
        }

        public void ExportPublicKeyAndAddress(string address, string filePath, string salt)
        {
            string extensionName = Path.GetExtension(filePath).ToLower();
            if (string.IsNullOrWhiteSpace(salt))
            {
                filePath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + noEncryptExtensionName);
            }
            else
            {
                filePath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + encryptExtensionName);
            }
            try
            {
                Account account = AccountDac.Default.SelectById(address);
                WatchAccountBackup backup = new WatchAccountBackup() { Address = account.Id, PublicKey = account.PublicKey };
                if (backup != null)
                {
                    if (extensionName == noEncryptExtensionName)
                    {
                        SaveFile(backup, filePath, null);
                    }
                    else
                    {
                        SaveFile(backup, filePath, salt);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CommonException(ErrorCode.Engine.Wallet.DATA_SAVE_TO_FILE_ERROR, ex);
            }
        }

        private static void SaveFile<T>(T obj, string filePath, string salt)
        {
            string jsonString = JsonConvert.SerializeObject(obj);
            string encryptString = jsonString;

            if (!string.IsNullOrEmpty(salt))
            {
                encryptString = AES128.Encrypt(jsonString, salt);
            }
            FileHelper.StringSaveFile(encryptString, filePath);
        }

        public Account ImportPublicKeyAndAddress(string filePath, string salt)
        {
            Account result = null;

            string extensionName = Path.GetExtension(filePath).ToLower();
            if (extensionName != encryptExtensionName && extensionName != noEncryptExtensionName)
            {
                throw new CommonException(ErrorCode.Engine.Wallet.IO.EXTENSION_NAME_NOT_SUPPORT);
            }
            WatchAccountBackup backup = null;
            try
            { 
                if (extensionName == noEncryptExtensionName)
                {
                    backup = LoadFile<WatchAccountBackup>(filePath, null);
                }
                else
                {
                    backup = LoadFile<WatchAccountBackup>(filePath, salt);
                }
                if (backup != null)
                {
                    AccountDac dac = AccountDac.Default;
                    dac.Insert(new Account { Balance = 0, Id = backup.Address, IsDefault = false, PrivateKey = null, PublicKey = backup.PublicKey, Tag = "", Timestamp = Time.EpochTime, WatchedOnly = true});
                    result = dac.SelectById(backup.Address);
                }
            }
            catch (Exception ex)
            {
                throw new CommonException(ErrorCode.Engine.Wallet.DB.EXECUTE_SQL_ERROR, ex);
            }
            return result;
        }

        private T LoadFile<T>(string filePath, string salt)
        {
            if (!File.Exists(filePath))
            {
                throw new CommonException(ErrorCode.Engine.Wallet.IO.FILE_NOT_FOUND);
            }
            string fileString = string.Empty;
            try
            {
                fileString = FileHelper.LoadFileString(filePath);
            }
            catch (Exception ex)
            {
                throw new CommonException(ErrorCode.Engine.Wallet.IO.FILE_DATA_INVALID, ex);
            }

            try
            {
                if (!string.IsNullOrEmpty(salt))
                {
                    fileString = AES128.Decrypt(fileString, salt);
                }
            }
            catch (Exception ex)
            {
                throw new CommonException(ErrorCode.Engine.Wallet.DECRYPT_DATA_ERROR, ex);
            }
            return JsonConvert.DeserializeObject<T>(fileString);
        }

        public List<TransOM> GetAccountDetailPage(string accountId, int skipCount, int takeCount)
        {
            List<TransOM> result = new List<TransOM>();
            var utxos = UtxoSetDac.Default.GetByAccounts(new string[] { accountId });
            if (utxos == null)
            {
                return result;
            }
            else
            {
                var totalUtxos = utxos.Where(x => x.Amount > 0);

                List<long> heights = utxos.Select(x => x.BlockHeight).ToList();
                var spentHeights = utxos.Select(x => x.SpentHeight).ToArray();
                if (spentHeights != null && spentHeights.Any())
                    heights.AddRange(spentHeights);
                heights = heights.Distinct().OrderByDescending(x => x).ToList();

                var skip = 0;
                foreach (var height in heights)
                {
                    if (result.Count == takeCount)
                        break;

                    var block = BlockDac.Default.SelectByHeight(height);
                    var txs = block.Transactions.Where(x => x.HasAddress(accountId));
                    if (txs.Any())
                    {
                        if (skip < skipCount)
                        {
                            var leftTxs = txs.Skip(skipCount - skip);
                            if (leftTxs.Any())
                            {
                                var trans = leftTxs.Select(x => x.ConvertToOM()).ToArray();
                                if (result.Count + trans.Length <= takeCount)
                                {
                                    result.AddRange(trans);
                                }
                                else
                                {
                                    var addTrans = trans.Take(takeCount - result.Count);
                                    result.AddRange(addTrans);
                                }

                                skip = skipCount;
                            }
                            else
                            {
                                skip += txs.Count();
                            }
                        }
                        else
                        {
                            var trans = txs.Select(x => x.ConvertToOM()).ToArray();
                            if (result.Count + trans.Length <= takeCount)
                            {
                                result.AddRange(trans);
                            }
                            else
                            {
                                var addTrans = trans.Take(takeCount - result.Count);
                                result.AddRange(addTrans);
                            }
                        }
                    }
                }
            }
            return result;
        }

        public AccountInfo GetAccountInfo(string accountId)
        {
            AccountInfo info = new AccountInfo();
            var utxos = UtxoSetDac.Default.GetByAccounts(new string[] { accountId });
            if (utxos == null)
            {
                info.AccountId = accountId;
                info.TotalAmount = 0;
                info.TransactionCount = 0;
                info.SurplusAmount = 0;
            }
            else
            {
                info.AccountId = accountId;
                var totalUtxos = utxos.Where(x => x.Amount > 0);

                var totalAmount = totalUtxos.Sum(x => x.Amount);
                info.TotalAmount = totalAmount;
                if (info.TotalAmount < 0)
                    info.TotalAmount = 0;

                try
                {
                    var unSpentAmount = DataStatisticsDac.Default.GetUnSpentAmount(accountId);
                    if (unSpentAmount == 0 && totalUtxos.Any())
                        unSpentAmount = totalUtxos.Where(x => !x.IsSpent).Sum(x => x.Amount);
                    info.SurplusAmount = unSpentAmount;
                    if (info.SurplusAmount < 0)
                        info.SurplusAmount = 0;
                }
                catch
                { }

                List<long> heights = utxos.Select(x => x.BlockHeight).ToList();
                var spentHeights = utxos.Select(x => x.SpentHeight).ToArray();
                if (spentHeights != null && spentHeights.Any())
                    heights.AddRange(spentHeights);
                heights = heights.Distinct().ToList();

                object obj = new object();
                Parallel.ForEach(heights, height =>
                {
                    var block = BlockDac.Default.SelectByHeight(height);
                    var txs = block.Transactions.Where(x => x.HasAddress(accountId));

                    Monitor.Enter(obj);
                    info.TransactionCount += txs.Count();
                    Monitor.Exit(obj);
                });
            }
            return info;
        }
    }
}
