


using OmniCoin.Data;
using OmniCoin.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.DataAgent
{
    public class BlacklistTxs
    {
        public static BlacklistTxs Current = BlacklistTxs.GetDefault();

        private const string containerName = "ErrorTransactions";
        private List<string> BlackList_Txs = new List<string>();
        object lockTxObj = new object();
        object lockTxFileObj = new object();


        private static BlacklistTxs GetDefault()
        {
            BlacklistTxs blacklist = new BlacklistTxs();
            var hashes = Storage.Instance.GetAllConfigData("BlackTxHashes");
            blacklist.Add(hashes);
            return blacklist;
        }

        public void Add(string hash)
        {
            lock (lockTxObj)
            {
                if (!BlackList_Txs.Contains(hash))
                {
                    BlackList_Txs.Add(hash);
                    Storage.Instance.UpdateAllConfigData("BlackTxHashes", this.BlackList_Txs.ToArray());
                    Storage.Instance.Delete(TransactionPool.containerName, hash);
                }
            }
        }

        public void AddToBlackFile(TransactionMsg txMsg)
        {
            if (txMsg == null)
                return;

            lock (lockTxFileObj)
            {
                Storage.Instance.PutData(containerName, txMsg.Hash, txMsg);
            }
        }

        public void Add(IEnumerable<string> hashs)
        {
            lock (lockTxObj)
            {
                bool isUpdate = false;
                foreach (var hash in hashs)
                {
                    if (!BlackList_Txs.Contains(hash))
                    {
                        BlackList_Txs.Add(hash);
                        isUpdate = true;
                    }
                }
                if (isUpdate)
                {
                    Storage.Instance.UpdateAllConfigData("BlackTxHashes", this.BlackList_Txs.ToArray());
                    hashs.ToList().ForEach(hash => Storage.Instance.Delete(TransactionPool.containerName, hash));
                }
            }
        }

        public bool IsBlacked(string hash)
        {
            return BlackList_Txs.Contains(hash);
        }

        public void Remove(string hash)
        {
            BlackList_Txs.Remove(hash);
            Storage.Instance.UpdateAllConfigData("BlackTxHashes", this.BlackList_Txs.ToArray());
        }
    }
}