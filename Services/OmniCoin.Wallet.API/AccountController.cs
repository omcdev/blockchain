

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using EdjCase.JsonRpc.Router.Abstractions;
using OmniCoin.Business;
using OmniCoin.Consensus;
using OmniCoin.Data.Dacs;
using OmniCoin.DTO;
using OmniCoin.Entities;
using OmniCoin.Framework;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OmniCoin.Wallet.API
{
    public class AccountController : BaseRpcController
    {
        private IMemoryCache _cache;

        public AccountController(IMemoryCache memoryCache) { _cache = memoryCache; }
        public IRpcMethodResult GetAccountByAddress(string address)
        {
            try
            {
                var component = new AccountComponent();
                var account = component.GetAccountById(address);
                if(account != null)
                {
                    var result = new AccountOM();
                    result.Address = account.Id;
                    result.PublicKey = account.PublicKey;
                    result.Balance = 0;
                    result.IsDefault = account.IsDefault;
                    result.WatchOnly = account.WatchedOnly;
                    result.Tag = account.Tag;

                    return Ok(result);
                }
                else
                {
                    throw new CommonException(ErrorCode.Service.Account.ACCOUNT_NOT_FOUND);
                }
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetAddressesByTag(string tag)
        {
            try
            {
                var accountComponent = new AccountComponent();
                var accounts = accountComponent.GetAccountsByTag(tag);
                var result = new List<AccountOM>();

                foreach(var account in accounts)
                {
                    var item = new AccountOM();
                    item.Address = account.Id;
                    item.PublicKey = account.PublicKey;
                    item.Balance = accountComponent.GetCondfirmedBalance(account.Id); 
                    item.IsDefault = account.IsDefault;
                    item.WatchOnly = account.WatchedOnly;
                    item.Tag = account.Tag;

                    result.Add(item);
                }

                return Ok(result.ToArray());
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetNewAddress(string tag)
        {
            try
            {
                var accountComponent = new AccountComponent();
                Account account = null;
                var setting = new SettingComponent().GetSetting();
                AccountOM result = null;

                if (setting.Encrypt)
                {
                    if (!string.IsNullOrWhiteSpace(_cache.Get<string>("WalletPassphrase")))
                    {
                        account = accountComponent.GenerateNewAccount();
                        account.IsDefault = true;
                        account.PrivateKey = AES128.Encrypt(account.PrivateKey, _cache.Get<string>("WalletPassphrase"));
                        accountComponent.UpdatePrivateKeyAr(account);
                    }
                    else
                    {
                        throw new CommonException(ErrorCode.Service.Wallet.WALLET_HAS_BEEN_LOCKED);
                    }
                }
                else
                {
                    account = accountComponent.GenerateNewAccount();
                    account.IsDefault = true;
                }

                if (account != null)
                {
                    account.Tag = tag;
                    accountComponent.UpdateTag(account.Id, tag);

                    result = new AccountOM();
                    result.Address = account.Id;
                    result.PublicKey = account.PublicKey;
                    result.Balance = account.Balance;
                    result.IsDefault = account.IsDefault;
                    result.WatchOnly = account.WatchedOnly;
                    result.Tag = account.Tag;
                }

                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult SetAccountTag(string address, string tag)
        {
            try
            {
                new AccountComponent().UpdateTag(address, tag);

                return Ok();
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetDefaultAccount()
        {
            try
            {
                var component = new UserSettingComponent();
                var id = component.GetDefaultAccount();

                var accountComponent = new AccountComponent();
                Account account = null;
                if (string.IsNullOrEmpty(id))
                {
                    account = accountComponent.GetAccountById(id);
                }
                else
                {
                    account = accountComponent.GetDefaultAccount();
                }

                if (account != null)
                {
                    var result = new AccountOM();
                    result.Address = account.Id;
                    result.PublicKey = account.PublicKey;
                    result.Balance = account.Balance;
                    result.IsDefault = account.IsDefault;
                    result.WatchOnly = account.WatchedOnly;
                    result.Tag = account.Tag;

                    return Ok(result);
                }
                else
                {
                    throw new CommonException(ErrorCode.Service.Account.ACCOUNT_NOT_FOUND);
                }
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        /// <summary>
        /// 分类获取账号，1：所有找零账户，2：所有创建账户，3：所有观察者账户，0或者其他：所有账户信息
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public IRpcMethodResult GetAccountCategory(int category)
        {
            try
            {
                List<Account> result = new AccountComponent().GetAccountCategory(category);
                result.ForEach(x => x.PrivateKey = null);
                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult SetDefaultAccount(string address)
        {
            try
            {
                var accountComponent = new AccountComponent();
                var account = accountComponent.GetAccountById(address);

                if(account != null)
                {
                    var component = new UserSettingComponent();
                    component.SetDefaultAccount(account.Id);
                }
                else
                {
                    throw new CommonException(ErrorCode.Service.Account.ACCOUNT_NOT_FOUND);
                }

                return Ok();
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult ValidateAddress(string address)
        {
            try
            {
                var result = new ValidateAddressOM();
                var accountComponent = new AccountComponent();

                result.address = address;

                if(AccountIdHelper.AddressVerify(address))
                {
                    result.isValid = true;
                    var account = accountComponent.GetAccountById(address);

                    if (account != null)
                    {
                        result.isMine = true;
                        result.isWatchOnly = string.IsNullOrWhiteSpace(account.PrivateKey);
                    }
                    else
                    {
                        result.isMine = false;
                    }

                    result.scriptPubKey = Script.BuildLockScipt(address);
                    result.isScript = false;
                    result.script = "P2PKH";
                    result.hex = null;
                    result.addresses = null;
                    result.pubKey = account?.PublicKey;
                    result.isCompressed = false;
                    result.account = account?.Tag;
                    result.hdKeyPath = null;
                    result.hdMasterKeyId = null;
                }
                else
                {
                    result.isValid = false;
                }

                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IRpcMethodResult ExportAddresses()
        {
            try
            {
                AccountComponent component = new AccountComponent();
                List<Account> accounts = component.GetAllAccounts();
                var list = accounts.Select(q => new { Id = q.Id, PublicKey = q.PublicKey });
                return Ok(Newtonsoft.Json.JsonConvert.SerializeObject(list));
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult ImportAddresses(string jsontext)
        {
            try
            {
                List<Account> accounts = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Account>>(jsontext);
                AccountComponent component = new AccountComponent();
                foreach (var item in accounts)
                {
                    Account account = new Account();
                    account.Id = item.Id;
                    account.Balance = 0;
                    account.IsDefault = false;
                    account.PrivateKey = "";
                    account.PublicKey = item.PublicKey;
                    account.Tag = "";
                    account.Timestamp = Time.EpochTime;
                    account.WatchedOnly = true;

                    AccountDac.Default.Insert(account);
                }

                return Ok();
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult AddWatchOnlyAddress(string publickey)
        {
            try
            {
                AccountComponent component = new AccountComponent();
                Account result = component.GenerateWatchOnlyAddress(publickey);
                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetPageAccountCategory(int category, int pageSize = 0, int pageCount = int.MaxValue, bool isSimple = true)
        {
            try
            {
                var accountCompent = new AccountComponent();
                List<Account> result = accountCompent.GetAllAccounts(); ;
                switch (category)
                {
                    case 1:
                        result = result.Where(x => x.IsDefault).ToList();
                        break;
                    case 2:
                        result = result.Where(x => !x.IsDefault).ToList();
                        break;
                    case 3:
                        result = result.Where(x => string.IsNullOrEmpty(x.PrivateKey)).ToList();
                        break;
                    default:
                        break;
                }

                var accounts = result.OrderBy(x => x.Id).Skip(pageSize * pageCount).Take(pageCount);

                if (isSimple)
                    return Ok(new { Count = result.Count.ToString(), Accounts = accounts.Select(x => new { x.Id, x.Tag }) });
                else
                    return Ok(new { Count = result.Count.ToString(), Accounts = accounts });
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }
    }
}
