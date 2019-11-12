

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Consensus;
using OmniCoin.Data;
using OmniCoin.Data.Dacs;
using OmniCoin.Data.Entities;
using OmniCoin.Entities;
using OmniCoin.Entities.CacheModel;
using OmniCoin.Entities.ExtensionModels;
using OmniCoin.Framework;
using OmniCoin.Messages;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace OmniCoin.DataAgent
{
    public class TransactionPool
    {
        private static TransactionPool instance;
        public const string containerName = "Transaction";

        public static void Remove(string name)
        {
            Storage.Instance.Delete(containerName, name);
        }

        private string GetAddress(string lockScript)
        {
            var publicKeyHash = Base16.Decode(Script.GetPublicKeyHashFromLockScript(lockScript));
            var address = AccountIdHelper.CreateAccountAddressByPublicKeyHash(publicKeyHash);
            return address;
        }

        public TransactionPool()
        {

        }
        /// <summary>
        /// 删除双花的交易
        /// </summary>
        /// <param name="costItems"></param>
        public void ClearCostUtxo(List<string> costItems = null)
        {
            var items = TransactionPoolDac.Default.GetAllTx();
            var costTx = items.Where(x => x.Transaction.Inputs.Any(input => costItems.Contains($"{input.OutputTransactionHash}_{input.OutputIndex}")));
            foreach (var item in costTx)
            {
                BlacklistTxs.Current.Add(item.Transaction.Hash);
                BlacklistTxs.Current.AddToBlackFile(item.Transaction);
                this.RemoveTransaction(item.Transaction.Hash);
            }
        }

        public void Load()
        {
            LogHelper.Debug("Load TransactionPool");
            var keys = Storage.Instance.GetAllKeys(containerName);

            var items = keys.Select(key => Storage.Instance.GetData<TransactionPoolItem>(containerName, key)).ToList();
            items.RemoveAll(x => null == x);
            //remove BlackHash
            var blackeds = items.Where(x => BlacklistTxs.Current.IsBlacked(x.Transaction.Hash) || !x.Transaction.Inputs.Any(p =>
            {
                var utxo = UtxoSetDac.Default.Get(p.OutputTransactionHash, p.OutputIndex);
                return utxo != null && !utxo.IsSpent();
            }));
            foreach (var blacked in blackeds)
            {
                BlacklistTxs.Current.AddToBlackFile(blacked.Transaction);
                BlacklistTxs.Current.Add(blacked.Transaction.Hash);
                Storage.Instance.Delete(containerName, blacked.Transaction.Hash);
            }
            items.RemoveAll(x => BlacklistTxs.Current.IsBlacked(x.Transaction.Hash));

            items.RemoveAll(x => TransactionDac.Default.HasTransaction(x.Transaction.Hash));

            var mainPoolItems = items;
            var results = TransactionPoolDac.Default.Insert(mainPoolItems);
            if (results.Any())
            {
                var accounts = AccountDac.Default.GetAccountBook();
                var addMsgs = mainPoolItems.Where(x => results.Contains(x.Transaction.Hash));
                var payments = addMsgs.SelectMany(x => x.Transaction.GetPayments(accounts));
                if (payments.Any())
                {
                    PaymentDac.Default.InsertMem(payments);
                }
            }

            InitIsolate();
        }

        Func<string, bool> UtxoIsSpentFunc = key => UtxoSetDac.Default.Get(new string[] { key }).FirstOrDefault().IsSpent();

        public static TransactionPool Instance
        {
            get
            {
                if (instance == null)
                    instance = new TransactionPool();
                return instance;
            }
        }

        public int Count
        {
            get
            {
                return TransactionPoolDac.Default.GetCount();
            }
        }

        public void AddNewTransaction(long feeRate, TransactionMsg transaction)
        {
            var item = new TransactionPoolItem(feeRate, transaction);
            if (TransactionPoolDac.Default.Insert(item))
            {
                Storage.Instance.PutData(containerName, transaction.Hash, item);
                var payments = transaction.GetPayments();
                PaymentDac.Default.InsertMem(payments);
            }
        }

        public void RemoveTransaction(string txHash)
        {
            TransactionPoolDac.Default.Del(txHash);
        }

        public void RemoveTransactions(IEnumerable<string> txHashes)
        {
            TransactionPoolDac.Default.Del(txHashes);
        }

        public TransactionMsg GetMaxFeeRateTransaction()
        {
            var items = TransactionPoolDac.Default.GetAllTx();
            if (items.Count > 0)
            {
                var item = items.OrderByDescending(t => t.FeeRate).FirstOrDefault();

                if (item != null)
                {
                    return item.Transaction;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public List<string> GetAllTransactionHashes()
        {
            var hashes = TransactionPoolDac.Default.GetAllHashes();
            hashes.AddRange(IsolateTxHashes);
            return hashes;
        }

        public TransactionMsg GetTransactionByHash(string hash)
        {
            if (IsolateTxHashes.Contains(hash))
                return Storage.Instance.GetData<TransactionMsg>(IsolateTxsContainerName, hash);
            return TransactionPoolDac.Default.Get(hash);
        }

        public bool HasCostUtxo(IEnumerable<string> hashIndexs)
        {
            foreach (var item in hashIndexs)
            {
                if (TransactionPoolDac.Default.UtxoHasSpent(item))
                    return true;
            }
            return false;
        }

        public string GetTxByHashIndex(string hash, int index)
        {
            return TransactionPoolDac.Default.GetSpentHash(hash, index);
        }

        /// <summary>
        /// 获取指定数量的未花费交易
        /// </summary>
        /// <param name="count"></param>
        public List<TransactionMsg> GetTxsWithoutRepeatCost(int count, long maxSize, int maxInputCount = 30)
        {
            var MainPool = TransactionPoolDac.Default.GetAllTx();
            //var tx1 = MainPool.Where(x => x.Transaction.InputCount <= 50);
            var txs = MainPool.OrderByDescending(x => x.FeeRate).ToArray();

            BlockMsg blockMsg = new BlockMsg();
            List<TransactionPoolItem> errorTxMsgs = new List<TransactionPoolItem>();

            List<TransactionMsg> result = new List<TransactionMsg>();
            List<string> utxos = new List<string>();
            int num = 0;
            int index = 0;
            int size = 0;
            while (num < count && size < maxSize)
            {
                if (txs.Length <= index)
                    break;

                var tx = txs[index];

                var hashIndexs = tx.Transaction.Inputs.Select(input => $"{input.OutputTransactionHash}_{input.OutputIndex}");
                foreach (var hi in hashIndexs)
                {
                    LogHelper.Warn($"tx hash:{tx.Transaction.Hash} Index：{index}, hashindex :{hi}");
                }

                //拿到可用的Utxo 数量                
                var utxosets = UtxoSetDac.Default.Get(hashIndexs);

                if (utxosets.Any() && utxosets.Count() < hashIndexs.Count())
                {
                    errorTxMsgs.Add(tx);
                    index++;
                    continue;
                }

                //判断确认次数
                var localHeight = GlobalParameters.LocalHeight;
                if (utxosets.Any(x => x.IsSpent()))
                {
                    errorTxMsgs.Add(tx);
                    index++;
                    continue;
                }
                if (utxosets.Any(x => !x.IsWaiting(localHeight, Time.EpochTime)))
                {
                    index++;
                    continue;
                }

                if (!result.Any())
                {
                    if (size + tx.Transaction.Size >= maxSize)
                        break;
                    result.Add(tx.Transaction);

                    utxos.AddRange(hashIndexs);
                    num++;
                    if (utxos.Count >= maxInputCount)
                        break;
                }
                else
                {
                    if (utxos.Any(p => hashIndexs.Contains(p))) //已经被花费
                    {
                        index++;
                        continue;
                    }
                    else
                    {
                        result.Add(tx.Transaction);
                        utxos.AddRange(hashIndexs);
                        num++;
                        if (utxos.Count >= maxInputCount)
                            break;
                    }
                }

                index++;
            }

            //矿池需要删除掉无效的交易
            if (errorTxMsgs.Any())
            {
                var errorHashes = errorTxMsgs.Select(x => x.Transaction.Hash);
                BlacklistTxs.Current.Add(errorHashes);
                foreach (var errorMsg in errorTxMsgs)
                {
                    BlacklistTxs.Current.AddToBlackFile(errorMsg.Transaction);
                    this.RemoveTransaction(errorMsg.Transaction.Hash);
                }
            }

            return result;
        }


        private List<string> IsolateTxHashes = new List<string>();
        object isolateObj = new object();
        const string IsolateTxsContainerName = "IsolateTxs";
        Thread IsolateTxsExecThread;
        private Dictionary<string, UtxoSet> MyIsolateUtxoSets = new Dictionary<string, UtxoSet>();
        private List<string> IsolateSpentUtxos = new List<string>();

        public void InitIsolate()
        {
            var keys = Storage.Instance.GetAllKeys(IsolateTxsContainerName);
            IsolateTxHashes.AddRange(keys);
            foreach (var item in keys)
            {
                var msg = Storage.Instance.GetData<TransactionMsg>(IsolateTxsContainerName, item);
                var myUtxos = msg.GetUtxoSets();
                foreach (var myOutput in myUtxos)
                {
                    var key = $"{myOutput.TransactionHash}_{myOutput.Index}";
                    MyIsolateUtxoSets.Add(key, myOutput);
                }

                var spents = msg.Inputs.Select(x => $"{x.OutputTransactionHash}_{x.OutputIndex}").ToArray();
                IsolateSpentUtxos.AddRange(spents);
            }
            if (IsolateTxsExecThread == null || !IsolateTxsExecThread.IsAlive)
            {
                IsolateTxsExecThread = new Thread(new ParameterizedThreadStart(Start));
                IsolateTxsExecThread.Start();
            }
        }

        public List<UtxoSet> GetMyUtxoSet()
        {
            var keys = MyIsolateUtxoSets.Keys.ToList();
            var poolUtxos = TransactionPoolDac.Default.GetMyUtxos();
            poolUtxos.RemoveAll(x => IsolateSpentUtxos.Contains($"{x.TransactionHash}_{x.Index}"));
            keys.RemoveAll(x => IsolateSpentUtxos.Contains(x));
            var utxos = MyIsolateUtxoSets.Where(x => keys.Contains(x.Key)).Select(x => x.Value).ToList();
            poolUtxos.AddRange(utxos);
            return poolUtxos;
        }

        public List<UtxoSet> GetMyUtxoSet(IEnumerable<string> hashIndexs)
        {
            var sets = MyIsolateUtxoSets.ToList();
            return sets.Where(x => hashIndexs.Contains(x.Key)).Select(x => x.Value).ToList();
        }

        public UtxoSet GetMyUtxoSet(string hash, int index)
        {
            var set = TransactionPoolDac.Default.GetMyUtxo(hash, index);
            if (set != null)
                return set;
            var hashIndex = $"{hash}_{index}";
            if (!MyIsolateUtxoSets.ContainsKey(hashIndex))
                return null;
            else
                return MyIsolateUtxoSets[hashIndex];
        }



        public void AddTxNoOutput(TransactionMsg transaction)
        {
            if (IsolateTxHashes.Contains(transaction.Hash))
                return;

            Storage.Instance.PutData(IsolateTxsContainerName, transaction.Hash, transaction);

            var myAccounts = AccountDac.Default.GetAccountBook();
            var outputs = transaction.GetUtxoSets();
            var myOutputs = outputs.Where(x => myAccounts.Contains(x.Account));

            var spents = transaction.Inputs.Select(x => $"{x.OutputTransactionHash}_{x.OutputIndex}").ToArray();
            Monitor.Enter(isolateObj);
            IsolateTxHashes.Add(transaction.Hash);
            foreach (var myOutput in myOutputs)
            {
                var key = $"{myOutput.TransactionHash}_{myOutput.Index}";
                MyIsolateUtxoSets.Add(key, myOutput);
            }

            IsolateSpentUtxos.AddRange(spents);
            Monitor.Exit(isolateObj);

            if (IsolateTxsExecThread == null || !IsolateTxsExecThread.IsAlive)
            {
                IsolateTxsExecThread = new Thread(new ParameterizedThreadStart(Start));
                IsolateTxsExecThread.Start();
            }
        }

        void Start(object obj)
        {
            const int sleepTime = 1000 * 60;
            const long ExpiredTime = 1000 * 60 * 60 * 24 * 7;
            while (true)
            {
                var items = IsolateTxHashes.ToArray();
                var localTime = Time.EpochTime;
                foreach (var item in items)
                {
                    var file = Path.Combine(DbDomains.StorageFile, IsolateTxsContainerName, item);
                    var lastTime = Time.GetEpochTime(File.GetLastWriteTime(file).ToUniversalTime());
                    var msg = Storage.Instance.GetData<TransactionMsg>(IsolateTxsContainerName, item);

                    if (localTime - lastTime > ExpiredTime)
                    {
                        Storage.Instance.Delete(IsolateTxsContainerName, item);
                        BlacklistTxs.Current.Add(item);
                        BlacklistTxs.Current.AddToBlackFile(msg);
                        IsolateTxHashes.Remove(item);
                    }
                    else
                    {
                        try
                        {
                            long txFee, totalOutput, totalInput;
                            var result = VerifyTransaction(msg, out txFee, out totalOutput, out totalInput);
                            if (result)
                            {
                                var feeRate = (totalInput - totalOutput) / msg.Size;
                                AddNewTransaction(feeRate, msg);
                                IsolateTxHashes.Remove(item);
                                Storage.Instance.Delete(IsolateTxsContainerName, item);
                                var removeKeys = MyIsolateUtxoSets.Where(x => x.Key.StartsWith(item)).Select(x=>x.Key).ToList();
                                removeKeys.ForEach(x => MyIsolateUtxoSets.Remove(x));
                            }
                        }
                        catch(Exception ex)
                        {
                            LogHelper.Error(ex.ToString());
                        }
                    }
                }
                Monitor.Enter(isolateObj);
                var isNull = IsolateTxHashes != null && IsolateTxHashes.Any();
                Monitor.Exit(isolateObj);
                if(isNull)
                    Thread.Sleep(sleepTime);
            }
        }

        public bool VerifyTransaction(TransactionMsg transaction, out long txFee, out long totalOutput, out long totalInput)
        {
            totalOutput = 0;
            totalInput = 0;
            txFee = 0;
            totalOutput = transaction.Outputs.Sum(x => x.Amount);

            foreach (var input in transaction.Inputs)
            {
                if (!Script.VerifyUnlockScriptFormat(input.UnlockScript))
                {
                    throw new CommonException(ErrorCode.Engine.Transaction.Verify.SCRIPT_FORMAT_ERROR);
                }

                var utxo = UtxoSetDac.Default.Get(input.OutputTransactionHash, input.OutputIndex);
                if (utxo != null)
                {
                    if ( !utxo.IsConfirmed(GlobalParameters.LocalHeight))
                    {
                        if (utxo.IsCoinbase)
                            throw new CommonException(ErrorCode.Engine.Transaction.Verify.COINBASE_NEED_100_CONFIRMS);
                        else
                            return false;
                    }

                    if (Time.EpochTime < utxo.Locktime)
                    {
                        throw new CommonException(ErrorCode.Engine.Transaction.Verify.TRANSACTION_IS_LOCKED);
                    }

                    if (utxo.IsSpent())
                    {
                        throw new CommonException(ErrorCode.Engine.Transaction.Verify.UTXO_HAS_BEEN_SPENT);
                    }

                    if(Time.EpochTime < utxo.DepositTime)
                    {
                        throw new CommonException(ErrorCode.Engine.Transaction.Verify.DEPOSIT_TIME_NOT_EXPIRED);
                    }
                    if (!Script.VerifyLockScriptByUnlockScript(input.OutputTransactionHash, input.OutputIndex, utxo.LockScript, input.UnlockScript))
                    {
                        throw new CommonException(ErrorCode.Engine.Transaction.Verify.UTXO_UNLOCK_FAIL);
                    }

                    totalInput += utxo.Amount;
                }
                else
                {
                    txFee = 0;
                    return false;
                }
            }

            if (totalOutput >= totalInput)
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.OUTPUT_LARGE_THAN_INPUT);
            }

            if ((totalInput - totalOutput) < BlockSetting.TRANSACTION_MIN_FEE)
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.TRANSACTION_FEE_IS_TOO_FEW);
            }

            if (totalInput > BlockSetting.INPUT_AMOUNT_MAX)
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.INPUT_EXCEEDED_THE_LIMIT);
            }

            if (totalOutput > BlockSetting.OUTPUT_AMOUNT_MAX)
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.OUTPUT_EXCEEDED_THE_LIMIT);
            }

            txFee = totalInput - totalOutput;
            return true;
        }
    }
}