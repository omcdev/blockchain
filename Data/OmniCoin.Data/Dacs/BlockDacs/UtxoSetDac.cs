


using OmniCoin.Data.Entities;
using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OmniCoin.Data.Dacs
{
    public class UtxoSetDac : BlockDbBase<UtxoSetDac>
    {
        private Dictionary<string,long> MyUtxoBook_Confirmed;
        private Dictionary<string, long> MyUtxoBook_UnConfirmed;
        private Dictionary<long, List<string>> BlockMyOutput;
        public UtxoSetDac()
        {
            MyUtxoBook_Confirmed = new Dictionary<string, long>();
            MyUtxoBook_UnConfirmed = new Dictionary<string, long>();
            BlockMyOutput = new Dictionary<long, List<string>>();
        }

        public void Init()
        {
            var height = 0L;
            if (long.TryParse(BlockDomain.Get(BlockDataSetting.LatestBlockHeight), out height))
            {
                GlobalParameters.LocalHeight = height;
                var accounts = AccountDac.Default.GetAccountBook();
                var myUtxoKeys = GetUtxoSetKeysByAccounts(accounts);

                var localTime = Time.EpochTime;
                int index = 0;
                int count = myUtxoKeys.Count;

                //var origRow = Console.CursorTop;
                //var origCol = Console.CursorLeft;

                foreach (var myUtxoKey in myUtxoKeys)
                {
                    index++;
                    //Console.SetCursorPosition(origCol, origRow);
                    //Console.Write($"Load UtxoSet {index}/{count}");
                    if(index % 1000 == 0 || index == count)
                    {
                        LogHelper.Info($"Load UtxoSet {index}/{count}");
                    }
                    var myUtxo = Get(myUtxoKey);
                    if (BlockMyOutput.ContainsKey(myUtxo.BlockHeight))
                    {
                        BlockMyOutput[myUtxo.BlockHeight].Add(myUtxoKey);
                    }
                    else
                    {
                        BlockMyOutput.Add(myUtxo.BlockHeight, new List<string> { myUtxoKey });
                    }

                    if (myUtxo.IsSpent())
                        continue;

                    if (myUtxo.IsConfirmed(height) && !MyUtxoBook_Confirmed.ContainsKey(myUtxoKey) && localTime >= myUtxo.Locktime && myUtxo.Amount > 0)
                    {
                        MyUtxoBook_Confirmed.Add(myUtxoKey, myUtxo.Amount);
                    }
                    else if (!MyUtxoBook_UnConfirmed.ContainsKey(myUtxoKey))
                    {
                        MyUtxoBook_UnConfirmed.Add(myUtxoKey, myUtxo.Amount);
                    }
                }
            }
        }

        public UtxoSet Get(string hash, int index)
        {
            var key = GetKey(BlockTables.UtxoSet, hash + "_" + index);
            return BlockDomain.Get<UtxoSet>(key);
        }

        public UtxoSet Get(string hashindex)
        {
            var key = GetKey(BlockTables.UtxoSet, hashindex);
            return BlockDomain.Get<UtxoSet>(key);
        }

        public bool HasCost(string hash, int index)
        {
            var set = Get(hash, index);
            return set == null ? false : set.IsSpent();
        }

        public bool HasCost(string hashindex)
        {
            var set = Get(hashindex);
            return set == null ? false : set.IsSpent();
        }

        public IEnumerable<UtxoSet> GetFirstOutputByHash(IEnumerable<string> hashes)
        {
            var keys = hashes.Select(x => GetKey(BlockTables.UtxoSet, x + "_0"));
            return BlockDomain.Get<UtxoSet>(keys);
        }

        public IEnumerable<UtxoSet> Get(IEnumerable<string> hashIndexs)
        {
            var keys = hashIndexs.Select(x => GetKey(BlockTables.UtxoSet, x));
            return BlockDomain.Get<UtxoSet>(keys)?.ToList()??new List<UtxoSet>();
        }

        public Dictionary<string, long> GetAmount(IEnumerable<string> hashIndexs)
        {
            Dictionary<string, long> dic = new Dictionary<string, long>();
            foreach (var hashIndex in hashIndexs)
            {
                if (MyUtxoBook_UnConfirmed.ContainsKey(hashIndex))
                {
                    dic.Add(hashIndex, MyUtxoBook_UnConfirmed[hashIndex]);
                }
                else if (MyUtxoBook_Confirmed.ContainsKey(hashIndex))
                {
                    dic.Add(hashIndex, MyUtxoBook_Confirmed[hashIndex]);
                }
                else
                {
                    dic.Add(hashIndex, Get(hashIndex)?.Amount ?? 0);
                }
            }
            return dic;
        }

        public void Del(IEnumerable<string> hashIndexs)
        {
            var outputs = Get(hashIndexs);
            if (outputs.Any())
            {
                Dictionary<string, List<string>> addr = new Dictionary<string, List<string>>();
                foreach (var item in outputs)
                {
                    var hashIndex = $"{item.TransactionHash}_{item.Index}";
                    if (addr.ContainsKey(item.Account))
                    {
                        addr.Remove(hashIndex);
                    }
                    else
                    {
                        var accountKey = GetKey(BlockTables.Link_Account_Utxo, item.Account);
                        var addrUtxos = BlockDomain.Get<List<string>>(accountKey) ?? new List<string>();
                        addrUtxos.Remove(hashIndex);
                        addr.Add(item.Account, addrUtxos);
                    }
                }
                BlockDomain.Put(addr);
            }
            var keys = hashIndexs.Select(x => GetKey(BlockTables.UtxoSet, x));
            BlockDomain.Del(keys);
        }

        public void Del(string hash, int index)
        {
            Del(new string[] { $"{hash}_{index}" });
        }
        
        public void SetSpent(string hash, int index, long height)
        {
            var set = Get(hash, index);
            if (set == null)
                return;
            set.IsSpent = true;
            set.SpentHeight = height;
            MyUtxoBook_Confirmed.Remove($"{hash}_{index}");
            Put(set);
        }

        public void SetSpentInMem(string hash, int index)
        {
            var key = $"{hash}_{index}";
            MyUtxoBook_Confirmed.Remove(key);
            MyUtxoBook_UnConfirmed.Remove(key);
        }


        public void SetSpent(IEnumerable<string> hashIndexs, long height)
        {
            var sets = Get(hashIndexs);
            if (sets == null || !sets.Any())
                return;
            Dictionary<string, UtxoSet> updates = new Dictionary<string, UtxoSet>();
            foreach (var set in sets)
            {
                set.IsSpent = true;
                set.SpentHeight = height;
                var key = GetKey(BlockTables.UtxoSet, $"{set.TransactionHash}_{set.Index}");
                if (!updates.ContainsKey(key))
                    updates.Add(key, set);
            }
            if (updates.Any())
            {
                BlockDomain.Put(updates);

                foreach (var hashIndex in hashIndexs)
                {
                    MyUtxoBook_Confirmed.Remove(hashIndex);
                }
            }
        }

        public void Update(IEnumerable<UtxoSet> sets)
        {
            Dictionary<string, UtxoSet> dic = new Dictionary<string, UtxoSet>();
            foreach (var set in sets)
            {
                var hashIndex = set.TransactionHash + "_" + set.Index;
                var key = GetKey(BlockTables.UtxoSet, hashIndex);
                dic.Add(key, set);
            }
            BlockDomain.Put(dic);
        }

        public void Put(UtxoSet set)
        {
            Put(new UtxoSet[] { set });
        }

        public void AddMyOutputs(long height, List<string> hashIndexs)
        {
            if (BlockMyOutput.ContainsKey(height))
            {
                hashIndexs.RemoveAll(x => BlockMyOutput[height].Contains(x));
                BlockMyOutput[height].AddRange(hashIndexs);
            }
            else
            {
                BlockMyOutput.Add(height, hashIndexs);
            }
        }


        public void Put(IEnumerable<UtxoSet> sets)
        {
            Dictionary<string, UtxoSet> updates = new Dictionary<string, UtxoSet>();
            Dictionary<string, List<string>> accountLinks = new Dictionary<string, List<string>>();
            var accounts = AccountDac.Default.GetAccountBook();
            foreach (var set in sets)
            {
                var hashIndex = set.TransactionHash + "_" + set.Index;
                var key = GetKey(BlockTables.UtxoSet, hashIndex);
                if (!updates.ContainsKey(key))
                    updates.Add(key, set);

                var accountKey = GetKey(BlockTables.Link_Account_Utxo, set.Account);
                if (!accountLinks.ContainsKey(accountKey))
                {
                    var items = BlockDomain.Get<List<string>>(accountKey) ?? new List<string>();
                    if (!items.Contains(hashIndex))
                    {
                        items.Add(hashIndex);
                    }
                    accountLinks.Add(accountKey, items);
                }
                else if (!accountLinks[accountKey].Contains(hashIndex))
                {
                    accountLinks[accountKey].Add(hashIndex);
                }
                if (!set.IsSpent() && accounts.Contains(set.Account))
                {
                    if (set.IsConfirmed(GlobalParameters.LocalHeight))
                    {
                        if (!MyUtxoBook_Confirmed.ContainsKey(hashIndex))
                            MyUtxoBook_Confirmed.Add(hashIndex, set.Amount);
                    }
                    else if (!MyUtxoBook_UnConfirmed.ContainsKey(hashIndex))
                    {
                        MyUtxoBook_UnConfirmed.Add(hashIndex, set.Amount);
                    }
                }
            }
            BlockDomain.Put(updates);
            BlockDomain.Put(accountLinks);
        }

        public IEnumerable<UtxoSet> GetByAccounts(IEnumerable<string> accounts)
        {
            var keys = accounts.Select(x => GetKey(BlockTables.Link_Account_Utxo, x));
            var utxos = BlockDomain.Get<List<string>>(keys);
            if (utxos == null || !utxos.Any())
                return new List<UtxoSet>();
            var utxoKeys = utxos.SelectMany(x => x);
            return Get(utxoKeys);
        }

        public List<string> GetUtxoSetKeysByAccounts(IEnumerable<string> accounts)
        {
            var keys = accounts.Select(x => GetKey(BlockTables.Link_Account_Utxo, x));
            var utxos = new List<string>();
            foreach (var key in keys)
            {
                var accountUtxos = BlockDomain.Get<List<string>>(key);
                if (accountUtxos != null && accountUtxos.Any())
                    utxos.AddRange(accountUtxos);
            }
            return utxos;
        }

        public List<string> GetUtxoSetKeys(long start, long end)
        {
            return BlockMyOutput.Where(x => x.Key >= start && x.Key <= end).SelectMany(x => x.Value).ToList();
        }


        public IEnumerable<string> GetMyUnspentUtxoKeys()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            UpdateUtxoSet(GlobalParameters.LocalHeight);
            stopwatch.Stop();
            LogHelper.Warn($"[UpdateUtxoSet] use time :: {stopwatch.ElapsedMilliseconds}");
            return MyUtxoBook_Confirmed.Where(x => x.Value > 0).Select(x => x.Key);
        }

        public List<UtxoSet> GetMyUnspents()
        {
            var keys = GetMyUnspentUtxoKeys();
            return Get(keys)?.ToList() ?? new List<UtxoSet>();
        }

        public List<UtxoSet> GetAllUnspents()
        {
            var unConfimedKeys = MyUtxoBook_UnConfirmed.Where(x => x.Value > 0).Select(x => x.Key).ToList();
            var confimedKeys = MyUtxoBook_Confirmed.Where(x => x.Value > 0).Select(x => x.Key).ToList();
            unConfimedKeys.AddRange(confimedKeys);

            var unspents = unConfimedKeys.Distinct().ToArray();

            return Get(unspents)?.ToList() ?? new List<UtxoSet>();
        }

        public List<UtxoSet> GetIsolateUtxoSets()
        {
            var keys = MyUtxoBook_UnConfirmed.Keys;
            var utxos = Get(keys)?.ToList();
            return utxos;
        }

        public List<string> GetAllUnspentsHashIndex(long minAmount, long maxAmount, bool isDesc = false)
        {
            List<KeyValuePair<string, long>> sets = new List<KeyValuePair<string, long>>();
            sets.AddRange(MyUtxoBook_UnConfirmed.Where(x => x.Value >= minAmount && x.Value <= maxAmount));
            sets.AddRange(MyUtxoBook_Confirmed.Where(x => x.Value >= minAmount && x.Value <= maxAmount));

            List<string> hashIndexs = new List<string>();
            if (isDesc)
            {
                hashIndexs = sets.OrderByDescending(x => x.Value).Select(x => x.Key).ToList();
            }
            else
            {
                hashIndexs = sets.OrderBy(x => x.Value).Select(x => x.Key).ToList();
            }

            return hashIndexs.Distinct().ToList();
        }

        public List<string> GetAllUnspentsHashIndex(long startHeight, long endHeight,long minAmount, long maxAmount, bool isDesc = false)
        {
            var outputs = BlockMyOutput.Where(x => x.Key >= startHeight && x.Key <= endHeight).SelectMany(x => x.Value);
            if (outputs == null)
                return new List<string>();

            Dictionary<string, long> sets = new Dictionary<string, long>();

            foreach (var output in outputs)
            {
                if (MyUtxoBook_UnConfirmed.ContainsKey(output))
                {
                    var amount = MyUtxoBook_UnConfirmed[output];
                    if (amount >= minAmount && amount <= maxAmount)
                    {
                        sets.Add(output, amount);
                    }
                }
                else if (MyUtxoBook_Confirmed.ContainsKey(output))
                {
                    var amount = MyUtxoBook_Confirmed[output];
                    if (amount >= minAmount && amount <= maxAmount)
                    {
                        sets.Add(output, amount);
                    }
                }
            }

            List<string> hashIndexs = new List<string>();
            if (isDesc)
            {
                hashIndexs = sets.OrderByDescending(x => x.Value).Select(x => x.Key).ToList();
            }
            else
            {
                hashIndexs = sets.OrderBy(x => x.Value).Select(x => x.Key).ToList();
            }

            return hashIndexs.Distinct().ToList();
        }

        public List<string> GetMyHashIndexs(long startHeight, long endHeight)
        {
            var outputs= BlockMyOutput.Where(x => x.Key >= startHeight && x.Key <= endHeight).SelectMany(x => x.Value);
            return outputs.ToList();
        }

        public IEnumerable<UtxoSet> GetMyUnspents(int start, int limit)
        {
            var keys = MyUtxoBook_UnConfirmed.Keys;
            var utxos = Get(keys);
            var confimedUtxos = utxos.Where(x => !x.IsSpent() && x.Amount > 0 && x.IsConfirmed(GlobalParameters.LocalHeight));
            foreach (var confimedUtxo in confimedUtxos)
            {
                var key = $"{confimedUtxo.TransactionHash}_{confimedUtxo.Index}";
                MyUtxoBook_Confirmed.Add(key, confimedUtxo.Amount);
                MyUtxoBook_UnConfirmed.Remove(key);
            }

            var myUtxoBook = MyUtxoBook_Confirmed.OrderBy(x => x.Value).Skip(start).Take(limit).Select(x => x.Key);

            if (!myUtxoBook.Any())
                return new List<UtxoSet>();

            return Get(myUtxoBook);
        }

        public long GetTotalAmount(IEnumerable<string> hashIndexs)
        {
            var totalAmount = 0L;
            foreach (var hashIndex in hashIndexs)
            {
                if (MyUtxoBook_Confirmed.ContainsKey(hashIndex))
                {
                    totalAmount += MyUtxoBook_Confirmed[hashIndex];
                }
                else
                {
                    var utxo = Get(hashIndex);
                    if (utxo != null)
                    {
                        totalAmount += utxo.Amount;
                    }
                }
            }
            return totalAmount - TransactionPoolDac.Default.UseBalanceInPool;
        }

        public long GetConfirmedAmount()
        {
            UpdateUtxoSet(GlobalParameters.LocalHeight);
            return MyUtxoBook_Confirmed.Where(x => x.Value > 0).Sum(x => x.Value);
        }

        public long GetTotalUnConfirmedAmount()
        {
            var result = 0L;
            if (MyUtxoBook_UnConfirmed.Keys.Count == 0)
                return result;
            var height = GlobalParameters.LocalHeight;
            UpdateUtxoSet(height);
            return MyUtxoBook_UnConfirmed.Sum(x => x.Value);
        }
        /// <summary>
        /// 刷新等待中的余额
        /// </summary>
        /// <param name="height"></param>
        public void UpdateUtxoSet(long height)
        {
            if (MyUtxoBook_UnConfirmed.Keys.Count == 0)
                return;
            var utxos = Get(MyUtxoBook_UnConfirmed.Keys);
            var localTime = Time.EpochTime;
            foreach (var utxo in utxos)
            {
                var key = $"{utxo.TransactionHash}_{utxo.Index}";
                if (utxo.IsSpent())
                {
                    MyUtxoBook_UnConfirmed.Remove(key);
                    MyUtxoBook_Confirmed.Remove(key);
                }
                else
                {
                    if (utxo.IsConfirmed(height) && utxo.Locktime <= localTime)
                    {
                        MyUtxoBook_UnConfirmed.Remove(key);
                        if (!MyUtxoBook_Confirmed.ContainsKey(key))
                        {
                            MyUtxoBook_Confirmed.Add(key, utxo.Amount);
                        }
                    }
                }
            }
        }
    }
}