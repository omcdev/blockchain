


using OmniCoin.Data.Entities;
using OmniCoin.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Data.Dacs
{
    [Serializable]
    public class TransactionPoolItem
    {
        public TransactionPoolItem(long feeRate, TransactionMsg transaction)
        {
            this.FeeRate = feeRate;
            this.Transaction = transaction;
        }

        public TransactionMsg Transaction { get; set; }

        /// <summary>
        /// 费率 fiii/KB
        /// </summary>
        public long FeeRate { get; set; }
        public bool Isolate { get; set; }
    }



    /// <summary>
    /// 交易池
    /// </summary>
    public class TransactionPoolDac : AppDbBase<TransactionPoolDac>
    {
        private List<TransactionPoolItem> transactionMsgs;
        Dictionary<string, List<string>> spentUtxoSets;

        public long UseBalanceInPool = 0;
        public long AddBalanceInPool = 0;

        public TransactionPoolDac()
        {
            transactionMsgs = new List<TransactionPoolItem>();
            spentUtxoSets = new Dictionary<string, List<string>>();
        }

        public List<string> GetAllHashes()
        {
            return transactionMsgs.Select(x => x.Transaction.Hash).ToList();
        }

        public int GetCount()
        {
            return transactionMsgs.Count;
        }

        public List<TransactionPoolItem> GetAllTx()
        {
            return transactionMsgs.ToList();
        }

        #region TransactionItem
        public bool Insert(TransactionPoolItem msg)
        {
            if (transactionMsgs.Any(x => x.Transaction.Hash.Equals(msg.Transaction.Hash)))
                return false;

            var accounts = AccountDac.Default.GetAccountBook();
            long useAmount = 0;
            long addAmount = 0;
            foreach (var item in msg.Transaction.Inputs)
            {
                var key = $"{item.OutputTransactionHash}_{item.OutputIndex}";
                if (spentUtxoSets.ContainsKey(key))
                {
                    spentUtxoSets[key].Add(msg.Transaction.Hash);
                }
                else
                {
                    spentUtxoSets.Add(key, new List<string>() { msg.Transaction.Hash });
                }
            }

            var spentUtxos = msg.Transaction.Inputs.Select(item => $"{item.OutputTransactionHash}_{item.OutputIndex}");
            var spentSets = UtxoSetDac.Default.Get(spentUtxos);
            if (spentSets != null && spentSets.Any())
            {
                useAmount = spentSets.Where(x => accounts.Contains(x.Account)).Sum(x => x.Amount);
            }

            var addUtxos = msg.Transaction.Outputs.Where(x => accounts.Contains(DtoExtensions.GetAccountByLockScript(x.LockScript)));
            if (addUtxos.Any())
            {
                addAmount = addUtxos.Sum(x => x.Amount);
            }
            UseBalanceInPool += useAmount;
            AddBalanceInPool += addAmount;
            transactionMsgs.Add(msg);
            return true;
        }

        public IEnumerable<string> Insert(IEnumerable<TransactionPoolItem> msgs)
        {
            var hashes = transactionMsgs.Select(x => x.Transaction.Hash);
            var addMsgs = msgs.Where(x => !hashes.Contains(x.Transaction.Hash)).ToArray();
            if (!addMsgs.Any())
                return null??new List<string>();
            var result = addMsgs.Select(x => x.Transaction.Hash);
            foreach (var msg in addMsgs)
            {
                foreach (var item in msg.Transaction.Inputs)
                {
                    var key = $"{item.OutputTransactionHash}_{item.OutputIndex}";
                    if (spentUtxoSets.ContainsKey(key))
                    {
                        spentUtxoSets[key].Add(msg.Transaction.Hash);
                    }
                    else
                    {
                        spentUtxoSets.Add(key, new List<string>() { msg.Transaction.Hash });
                    }
                }
                transactionMsgs.Add(msg);
            }

            long useAmount = 0;
            long addAmount = 0;

            var inputs = addMsgs.SelectMany(x => x.Transaction.Inputs);
            var outputs = addMsgs.SelectMany(x => x.Transaction.Outputs);
            var accounts = AccountDac.Default.GetAccountBook();

            var spentUtxos = inputs.Select(item => $"{item.OutputTransactionHash}_{item.OutputIndex}");
            var spentSets = UtxoSetDac.Default.Get(spentUtxos);
            if (spentSets != null)
            {
                useAmount = spentSets.Where(x => accounts.Contains(x.Account)).Sum(x => x.Amount);
            }

            var addUtxos = outputs.Where(x => accounts.Contains(DtoExtensions.GetAccountByLockScript(x.LockScript)));
            addAmount = addUtxos.Sum(x => x.Amount);

            UseBalanceInPool += useAmount;
            AddBalanceInPool += addAmount;
            return result;
        }

        public void Del(string hash)
        {
            var item = transactionMsgs.FirstOrDefault(x => x.Transaction.Hash.Equals(hash));
            if (item == null)
                return;
            transactionMsgs.Remove(item);
            Storage.Instance.Delete(DbDomains.TxContainer, item.Transaction.Hash);
            var spentUtxos = item.Transaction.Inputs.Select(x => $"{x.OutputTransactionHash}_{x.OutputIndex}");
            foreach (var spentUtxo in spentUtxos)
            {
                if (spentUtxoSets.ContainsKey(spentUtxo))
                {
                    spentUtxoSets[spentUtxo].Remove(hash);
                }
            }

            long useAmount = 0;
            long addAmount = 0;

            var outputs = item.Transaction.Outputs;
            var accounts = AccountDac.Default.GetAccountBook();

            var spentSets = UtxoSetDac.Default.Get(spentUtxos);
            if (spentSets != null)
            {
                useAmount = spentSets.Where(x => accounts.Contains(x.Account)).Sum(x => x.Amount);
            }

            var addUtxos = outputs.Where(x => accounts.Contains(DtoExtensions.GetAccountByLockScript(x.LockScript)));
            addAmount = addUtxos.Sum(x => x.Amount);

            UseBalanceInPool -= useAmount;
            AddBalanceInPool -= addAmount;
            
            var delKeys = PaymentDac.Default.Payment_Mem.Where(x => hash.Equals(x.txId)).Select(x => x.ToString());
            PaymentDac.Default.Del(delKeys);
        }

        public void Del(IEnumerable<string> hashes)
        {
            var items = transactionMsgs.Where(x => hashes.Contains(x.Transaction.Hash)).ToList();
            if (!items.Any())
                return;
            items.ForEach(x =>
            {
                transactionMsgs.Remove(x);
                Storage.Instance.Delete(DbDomains.TxContainer, x.Transaction.Hash);
            });

            foreach (var item in items)
            {
                var hashIndexs = item.Transaction.Inputs.Select(x => $"{x.OutputTransactionHash}_{x.OutputIndex}");
                foreach (var spentUtxo in hashIndexs)
                {
                    if (spentUtxoSets.ContainsKey(spentUtxo))
                    {
                        spentUtxoSets[spentUtxo].Remove(item.Transaction.Hash);
                    }
                }
            }

            long useAmount = 0;
            long addAmount = 0;

            var inputs = items.SelectMany(x => x.Transaction.Inputs);
            var outputs = items.SelectMany(x => x.Transaction.Outputs);
            var accounts = AccountDac.Default.GetAccountBook();

            var spentUtxos = inputs.Select(item => $"{item.OutputTransactionHash}_{item.OutputIndex}");
            var spentSets = UtxoSetDac.Default.Get(spentUtxos);
            if (spentSets != null)
            {
                useAmount = spentSets.Where(x => accounts.Contains(x.Account)).Sum(x => x.Amount);
            }

            var addUtxos = outputs.Where(x => accounts.Contains(DtoExtensions.GetAccountByLockScript(x.LockScript)));
            addAmount = addUtxos.Sum(x => x.Amount);

            UseBalanceInPool -= useAmount;
            AddBalanceInPool -= addAmount;
            var delKeys = PaymentDac.Default.Payment_Mem.Where(x => hashes.Contains(x.txId)).Select(x => x.ToString());
            PaymentDac.Default.Del(delKeys);
        }

        public TransactionMsg Get(string hash)
        {
            var item = transactionMsgs.FirstOrDefault(x => x.Transaction.Hash.Equals(hash));
            if (item == null)
                return null;
            else
                return item.Transaction;
        }

        public bool IsExist(string hash)
        {
            return Get(hash) != null;
        }
        #endregion

        public void DelByHashIndex(string hashindex)
        {
            DelByHashIndex(new string[] { hashindex });
        }

        public void DelByHashIndex(IEnumerable<string> hashindexs)
        {
            var spents = spentUtxoSets.Where(x => hashindexs.Contains(x.Key)).ToList();
            if (!spents.Any())
                return;

            var hashes = spents.SelectMany(x => x.Value).ToList();

            var delMsgs = transactionMsgs.Where(x => hashes.Contains(x.Transaction.Hash)).ToList();

            long useAmount = 0;
            long addAmount = 0;

            var inputs = delMsgs.SelectMany(x => x.Transaction.Inputs);
            var outputs = delMsgs.SelectMany(x => x.Transaction.Outputs);
            var accounts = AccountDac.Default.GetAccountBook();

            var spentUtxos = inputs.Select(item => $"{item.OutputTransactionHash}_{item.OutputIndex}");
            var spentSets = UtxoSetDac.Default.Get(spentUtxos);
            if (spentSets != null)
            {
                useAmount = spentSets.Where(x => accounts.Contains(x.Account)).Sum(x => x.Amount);
            }

            var addUtxos = outputs.Where(x => accounts.Contains(DtoExtensions.GetAccountByLockScript(x.LockScript)));
            addAmount = addUtxos.Sum(x => x.Amount);

            UseBalanceInPool -= useAmount;
            AddBalanceInPool -= addAmount;

            delMsgs.ForEach(x => {
                transactionMsgs.Remove(x);
                Storage.Instance.Delete(DbDomains.TxContainer, x.Transaction.Hash);
            });
            
            foreach (var hashindex in hashindexs)
            {
                spentUtxoSets.Remove(hashindex);
            }

            var delKeys = PaymentDac.Default.Payment_Mem.Where(x => hashes.Contains(x.txId)).Select(x => x.ToString()).ToArray();
            PaymentDac.Default.Del(delKeys);
        }

        #region UtxoSpent
        public string GetUtxoKey(string hash, int index) => $"{hash}_{index}";

        public bool UtxoHasSpent(string hash, int index)
        {
            var key = GetUtxoKey(hash, index);
            if (spentUtxoSets.ContainsKey(key))
                return true;

            return UtxoSetDac.Default.Get(hash, index)?.IsSpent ?? false;
        }

        public string GetSpentHash(string hash, int index)
        {
            /*
            var key = GetUtxoKey(hash, index);
            if (spentUtxoSets.ContainsKey(key))
                return spentUtxoSets[key].FirstOrDefault();
                */

            var utxoSet = UtxoSetDac.Default.Get(hash, index);
            if (utxoSet == null)
                return null;
            if (!utxoSet.IsSpent)
            {
                var result = transactionMsgs.FirstOrDefault(x => x.Transaction.Inputs.Any(p => p.OutputTransactionHash.Equals(hash) && p.OutputIndex == index));
                return result?.Transaction.Hash;
            }
            else
            {
                var block = BlockDac.Default.SelectByHeight(utxoSet.SpentHeight);
                if (block == null)
                    return null;
                var result = block.Transactions.FirstOrDefault(x => x.Inputs.Any(p => p.OutputTransactionHash.Equals(hash) && p.OutputIndex == index));
                return result?.Hash;
            }
        }

        public bool UtxoHasSpent(string hashindex)
        {
            if (spentUtxoSets.ContainsKey(hashindex))
                return true;

            return UtxoSetDac.Default.Get(hashindex)?.IsSpent ?? false;
        }

        public List<string> GetSpentUtxo()
        {
            return spentUtxoSets.Keys.ToList();
        }
        #endregion

        public List<UtxoSet> GetMyUtxos()
        {
            var items = transactionMsgs.Select(x=>x.Transaction).ToArray();
            var utxos= items.SelectMany(x => x.GetUtxoSets());
            var myAccounts = AccountDac.Default.GetMyAccountBook();
            utxos = utxos.Where(x => myAccounts.Contains(x.Account));
            return utxos.ToList();
        }

        public UtxoSet GetMyUtxo(string hash,int index)
        {
            var item = transactionMsgs.FirstOrDefault(x=>x.Transaction.Hash.Equals(hash));
            if (item == null)
                return null;
            if (index >= item.Transaction.OutputCount)
                return null;

            var transaction = item.Transaction;
            var output = item.Transaction.Outputs[index];
            return new UtxoSet
            {
                BlockHeight = 0,
                IsCoinbase = false,
                IsSpent = false,
                Locktime = transaction.Locktime,
                TransactionHash = transaction.Hash,
                Index = output.Index,
                Amount = output.Amount,
                BlockHash = null,
                BlockTime = 0,
                TransactionTime = transaction.Timestamp,
                LockScript = output.LockScript,
                Account = DtoExtensions.GetAccountByLockScript == null ? null : DtoExtensions.GetAccountByLockScript(output.LockScript)
            };
        }
    }
}