// Copyright (c) 2018 FiiiLab Technology Ltd
// Distributed under the MIT software license, see the accompanying
// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using FiiiChain.Framework;
using FiiiChain.Messages;
using System.Collections.Generic;
using System.Linq;

namespace FiiiChain.DataAgent
{
    public class UtxoSet
    {
        protected Dictionary<string, SafeCollection<UtxoMsg>> MainSet;
        public static UtxoSet Instance;

        public UtxoSet()
        {
            this.MainSet = new Dictionary<string, SafeCollection<UtxoMsg>>();
        }

        public static void Initialize(string[] accountIds)
        {
            if(Instance == null)
            {
                Instance = new UtxoSet();
            }

            foreach(var id in accountIds)
            {
                Instance.AddAccountId(id);
            }
        }

        public void AddAccountId(string accountId)
        {
            if(!this.MainSet.ContainsKey(accountId))
            {
                this.MainSet.Add(accountId, new List<UtxoMsg>());
            }
        }

        public bool RemoveAccountId(string accountId)
        {
            return this.MainSet.Remove(accountId);
        }

        public List<string> GetAllAccountIds()
        {
            var result = new List<string>();

            foreach(var key in MainSet.Keys)
            {
                result.Add(key);
            }

            return result;
        }

        public void AddUtxoRecord(UtxoMsg utxo)
        {
            if (this.MainSet.ContainsKey(utxo.AccountId))
            {
                var item = this.MainSet[utxo.AccountId].Where(u => u.TransactionHash == utxo.TransactionHash &&
                u.OutputIndex == utxo.OutputIndex).FirstOrDefault();

                if (item == null)
                {
                    this.MainSet[utxo.AccountId].Add(utxo);
                }
                else
                {
                    item.BlockHash = utxo.BlockHash;
                    item.IsConfirmed = utxo.IsConfirmed;
                }

                if(GlobalActions.TransactionNotifyAction != null)
                {
                    GlobalActions.TransactionNotifyAction(utxo.TransactionHash);
                }
            }
        }

        public bool RemoveUtxoRecord(string transactionHash, int outputIndex)
        {
            foreach (var accountId in this.MainSet.Keys)
            {
                var utxo = this.MainSet[accountId].Where(u => u.TransactionHash == transactionHash &&
                    u.OutputIndex == outputIndex).
                    FirstOrDefault();
                
                if(utxo != null)
                {
                    return this.MainSet[accountId].Remove(utxo);
                }
            }

            return false;
        }

        public List<UtxoMsg> GetUtxoSetByAccountId(string accountId)
        {
            if(this.MainSet.ContainsKey(accountId))
            {
                return this.MainSet[accountId].ToList();
            }
            else
            {
                return null;
            }
        }

        public long GetAccountBlanace(string accountId, bool isConfirmed)
        {
            if(MainSet.ContainsKey(accountId))
            {
                return MainSet[accountId].Where(u => u.IsConfirmed == isConfirmed).Sum(u => u.Amount);
            }
            else
            {
                return 0;
            }
        }

        public long GetAllConfirmedBalance()
        {
            long balance = 0;

            foreach(var key in this.MainSet.Keys)
            {
                foreach(UtxoMsg utxo in this.MainSet[key])
                {
                    if(utxo.IsConfirmed)
                    {
                        balance += utxo.Amount;
                    }
                }
            }

            return balance;
        }
        public long GetUnConfirmedBalance()
        {
            long balance = 0;

            foreach (var key in this.MainSet.Keys)
            {
                foreach (UtxoMsg utxo in this.MainSet[key])
                {
                    if (!utxo.IsConfirmed)
                    {
                        balance += utxo.Amount;
                    }
                }
            }

            return balance;
        }

        public List<UtxoMsg> GetAllUnspentOutputs()
        {
            var list = new List<UtxoMsg>();

            foreach(var key in this.MainSet.Keys)
            {
                list.AddRange(MainSet[key].Where(u => u.IsConfirmed && !u.IsWatchedOnly).ToArray());
            }

            return list.OrderByDescending(u=>u.Amount).ToList();
        }
    }
}
