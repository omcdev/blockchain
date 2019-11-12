

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using EdjCase.JsonRpc.Router.Abstractions;
using OmniCoin.Business;
using OmniCoin.Data;
using OmniCoin.Data.Dacs;
using OmniCoin.Data.Entities;
using OmniCoin.DataAgent;
using OmniCoin.DTO;
using OmniCoin.DTO.Utxo;
using OmniCoin.Entities;
using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OmniCoin.Wallet.API
{
    public class UtxoController : BaseRpcController
    {
        public IRpcMethodResult GetTxOut(string txid, int vount, bool unconfirmed = false)
        {
            try
            {
                var transactionComponent = new TransactionComponent();
                var result = transactionComponent.GetTxOut(txid, vount);
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

        public IRpcMethodResult GetTxOutSetInfo()
        {
            try
            {
                var transactionComponent = new TransactionComponent();
                var result = transactionComponent.GetTxOutSetInfo();
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
        /// CoinEgg专用，获取数据库中为未费的所有总额
        /// </summary>
        /// <returns></returns>
        public IRpcMethodResult GetTotalBalance()
        {
            try
            {
                var transactionComponent = new TransactionComponent();
                var result = transactionComponent.GetTotalBalance();
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

        public IRpcMethodResult ListUnspent(int minConfirmations, int maxConfirmations = 9999999, string[] addresses = null)
        {
            try
            {
                var result = new List<ListUnspentOM>();
                var utxosets = UtxoSetDac.Default.GetAllUnspents();
                if (!utxosets.Any())
                    return Ok(result);

                var sets = TransactionPool.Instance.GetMyUtxoSet();
                if (sets.Any())
                {
                    sets.ForEach(x =>
                    {
                        x.BlockHeight = -1;
                        utxosets.Add(x);
                    });
                }
                                
                utxosets.RemoveAll(x => x.IsSpentInPool());
                var now = Time.EpochTime;
                utxosets = utxosets.Where(x => x.DepositTime == 0 || x.DepositTime < now).ToList();
                //var count = utxosets.Count();                
                
                foreach (var utxoset in utxosets)
                {
                    if (addresses == null || addresses.Contains(utxoset.Account))
                    {
                        if(!utxoset.IsSpent)
                        {                            
                            result.Add(new ListUnspentOM
                            {
                                account = utxoset.Account,
                                address = utxoset.Account,
                                amount = utxoset.Amount,
                                confirmations = GlobalParameters.LocalHeight - utxoset.BlockHeight+1,
                                scriptPubKey = utxoset.LockScript,
                                spendable = false,
                                txid = utxoset.TransactionHash,
                                vout = utxoset.Index
                            });
                        }
                        else
                        {
                            UtxoSetDac.Default.SetSpentInMem(utxoset.TransactionHash, utxoset.Index);
                        }
                    }
                }
                if (minConfirmations < 0)
                    minConfirmations = -1;
                
                result = result.Where(q => q.confirmations > minConfirmations && q.confirmations < maxConfirmations).OrderBy(q => q.confirmations).ToList();
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

        public IRpcMethodResult ListPageUnspent(long minConfirmations, int currentPage, int pageSize, long maxConfirmations = 9999999, long minAmount = 1, long maxAmount = long.MaxValue, bool isDesc = false)
        {
            try
            {
                if (minConfirmations < 0)
                    minConfirmations = 0;
                var currentHeight = GlobalParameters.LocalHeight;
                var startHeight = currentHeight - maxConfirmations;
                var endHeight = currentHeight - minConfirmations;

                var unspentOMList = new List<ListUnspentOM>();
                var utxosetKeys = UtxoSetDac.Default.GetAllUnspentsHashIndex(startHeight, endHeight, minAmount, maxAmount, isDesc);

                var spents = TransactionPoolDac.Default.GetSpentUtxo();
                if (spents != null && spents.Any())
                    utxosetKeys.RemoveAll(x => spents.Contains(x));

                long count = utxosetKeys.Count();
                var currentPageUtxosets = UtxoSetDac.Default.GetAmount(utxosetKeys).ToList();
                if (minConfirmations <= 0)
                {
                    var sets = TransactionPool.Instance.GetMyUtxoSet();
                    sets = sets.Where(x => x.Amount >= minAmount && x.Amount <= maxAmount).ToList();
                    count += sets.Count;
                    foreach (var set in sets)
                    {
                        currentPageUtxosets.Add(new KeyValuePair<string, long>($"{set.TransactionHash}_{set.Index}", set.Amount));
                    }
                }

                if (currentPageUtxosets == null || !currentPageUtxosets.Any())
                    return Ok(new ListPageUnspentOM());

                if (isDesc)
                {
                    currentPageUtxosets = currentPageUtxosets.OrderByDescending(x => x.Value).ToList();
                }
                else
                {
                    currentPageUtxosets = currentPageUtxosets.OrderBy(x => x.Value).ToList();
                }

                currentPageUtxosets = currentPageUtxosets.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

                var now = Time.EpochTime;
                foreach (var utxoKV in currentPageUtxosets)
                {
                    var utxoset = UtxoSetDac.Default.Get(utxoKV.Key);
                    if (utxoset == null)
                    {
                        utxoset = TransactionPool.Instance.GetMyUtxoSet(new string[] { utxoKV.Key }).FirstOrDefault();
                        if (utxoset != null)
                            utxoset.BlockHeight = -1;
                    }
                    if (utxoset == null)
                        continue;                    
                    if (!utxoset.IsSpent)
                    {
                        if (utxoset.DepositTime < now)
                        {
                            unspentOMList.Add(new ListUnspentOM
                            {
                                account = utxoset.Account,
                                address = utxoset.Account,
                                amount = utxoset.Amount,
                                confirmations = utxoset.BlockHeight >= 0 ? GlobalParameters.LocalHeight - utxoset.BlockHeight+1 : utxoset.BlockHeight,
                                scriptPubKey = utxoset.LockScript,
                                spendable = false,
                                txid = utxoset.TransactionHash,
                                vout = utxoset.Index
                            });
                        }                        
                    }
                    else
                    {
                        UtxoSetDac.Default.SetSpentInMem(utxoset.TransactionHash, utxoset.Index);
                    }
                }

                return Ok(new ListPageUnspentOM { UnspentOMList = unspentOMList, Count = count });
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
        /// 获取指定金额以下的UTXO（UTXO拆分合并专用）
        /// </summary>
        /// <param name="currentPage"></param>
        /// <param name="pageSize"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public IRpcMethodResult ListPageUnspentByAmount(int currentPage, int pageSize, long maxAmount, long minAmount = 0)
        {
            try
            {
                List<ListUnspentOM> unspentOMList = new List<ListUnspentOM>();
                var utxosets = UtxoSetDac.Default.GetMyUnspents();
                if (!utxosets.Any())
                    return Ok(new ListPageUnspentOM());

                utxosets.RemoveAll(x => x.Amount < minAmount || x.Amount>maxAmount || x.IsSpentInPool());
                var count = utxosets.Count();

                var currentHeight = GlobalParameters.LocalHeight;
                var now = Time.EpochTime;
                var currentPageUtxosets = utxosets.Where(x => x.DepositTime == 0 || x.DepositTime < now).Skip((currentPage - 1) * pageSize).Take(pageSize);
                foreach (var utxoset in currentPageUtxosets)
                {
                    if (!utxoset.IsSpent)
                    {
                        unspentOMList.Add(new ListUnspentOM
                        {
                            account = utxoset.Account,
                            address = utxoset.Account,
                            amount = utxoset.Amount,
                            confirmations = GlobalParameters.LocalHeight - utxoset.BlockHeight,
                            scriptPubKey = utxoset.LockScript,
                            spendable = false,
                            txid = utxoset.TransactionHash,
                            vout = utxoset.Index
                        });
                    }
                    else
                    {
                        UtxoSetDac.Default.SetSpentInMem(utxoset.TransactionHash, utxoset.Index);
                    }
                }

                return Ok(new ListPageUnspentOM { UnspentOMList = unspentOMList, Count = count });
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
        
        public IRpcMethodResult ListPageUnspentNew(int currentPage, int pageSize)
        {
            try
            {
                var result = new List<ListUnspentOM>();
                var utxosetKeys = UtxoSetDac.Default.GetMyUnspentUtxoKeys()?.ToList();
                if (utxosetKeys == null || !utxosetKeys.Any())
                    return Ok(new ListPageUnspentOM());

                utxosetKeys.RemoveAll(x => TransactionPoolDac.Default.UtxoHasSpent(x));
                if (!utxosetKeys.Any())
                    return Ok(new ListPageUnspentOM());

                var now = Time.EpochTime;
                var utxosets = UtxoSetDac.Default.Get(utxosetKeys).Where(x => x.DepositTime == 0 || x.DepositTime < now); 
                
                
                var currentHeight = GlobalParameters.LocalHeight;
                var localTime = Time.EpochTime;
                var count = utxosets.Count();

                var currentPageUtxosets = utxosets.Skip((currentPage - 1) * pageSize).Take(pageSize);
                foreach (var utxoset in currentPageUtxosets)
                {
                    if (!utxoset.IsSpent)
                    {
                        result.Add(new ListUnspentOM
                        {
                            account = utxoset.Account,
                            address = utxoset.Account,
                            amount = utxoset.Amount,
                            confirmations = GlobalParameters.LocalHeight - utxoset.BlockHeight,
                            scriptPubKey = utxoset.LockScript,
                            spendable = false,
                            txid = utxoset.TransactionHash,
                            vout = utxoset.Index
                        });
                    }
                    else
                    {
                        UtxoSetDac.Default.SetSpentInMem(utxoset.TransactionHash, utxoset.Index);
                    }
                }

                return Ok(new ListPageUnspentOM { UnspentOMList = result, Count = count });
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
        
        public IRpcMethodResult GetUnconfirmedBalance()
        {
            try
            {
                var transactionComponent = new TransactionComponent();
                var result = transactionComponent.GetUnconfimedAmount();
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
        /// 锁定未花费的输出
        /// </summary>
        /// <param name="isLocked">true为已锁定，false为未锁定</param>
        /// <param name="transaction">需要锁定的交易</param>
        /// <returns></returns>
        public IRpcMethodResult LockUnspent(bool isLocked, ListLockUnspentOM[] transaction)
        {
            try
            {
                /* 1、把输入的参数用个静态变量存储起来
                 * 2、当转账或其他交易的时候先判断是否在静态变量中存在，如果存在就跳过，
                 * 3、注意修改对应的转账接口
                 */
                //根据Transaction的txid和vout获取outputList的ReceivedId
                TransactionComponent component = new TransactionComponent();
                AccountComponent account = new AccountComponent();
                List<string> accountIdList = AccountDac.Default.GetAccountBook();
                foreach (var item in transaction)
                {
                    string receivedId = component.GetUtxoSetByIndexAndTxHash(item.txid, item.vout)?.Account;
                    //只锁定自己的账户
                    if (accountIdList.Contains(receivedId))
                    {
                        if (isLocked)
                        {
                            Startup.lockUnspentList.Add(item);
                        }
                        else
                        {
                            Startup.lockUnspentList.Remove(Startup.lockUnspentList.FirstOrDefault(p => p.txid == item.txid && p.vout == item.vout));
                        }
                    }
                }
                //去除重复数据
                Startup.lockUnspentList = Startup.lockUnspentList.GroupBy(p => new { p.txid, p.vout }).Select(q => q.First()).ToList();
                return Ok(isLocked);
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
        /// 列出锁定的未花费
        /// </summary>
        /// <returns>ListLockUnspentOM对象数组</returns>
        public IRpcMethodResult ListLockUnspent()
        {
            try
            {
                return Ok(Startup.lockUnspentList);
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

        public IRpcMethodResult SetAutoAddress(bool isEnable)
        {
            try
            {
                UserSettingComponent userSettingComponent = new UserSettingComponent();
                userSettingComponent.SetEnableAutoAccount(isEnable);
                return Ok();
            }
            catch (Exception ex)
            {
                return Error(ex.HResult, ex.Message, ex);
            }
        }
    }
}