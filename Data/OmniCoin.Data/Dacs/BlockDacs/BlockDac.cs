


using OmniCoin.Data.Entities;
using OmniCoin.Entities;
using OmniCoin.Entities.CacheModel;
using OmniCoin.Entities.ExtensionModels;
using OmniCoin.Framework;
using OmniCoin.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OmniCoin.Data.Dacs
{
    public class BlockDac : BlockDbBase<BlockDac>
    {
        public Func<string, string> GetAccountByLockScript;

        public virtual void SetLastBlockHeight(long height)
        {
            BlockDomain.Put(BlockDataSetting.LatestBlockHeight, height.ToString());
        }

        public virtual void SaveBlockOnly(BlockMsg block)
        {
            var key = GetKey(BlockTables.Block, block.Header.Hash);
            BlockDomain.Put(key, block);
        }

        public virtual void Save(BlockMsg block)
        {
            //先存入区块信息
            var key = GetKey(BlockTables.Block, block.Header.Hash);
            BlockDomain.Put(key, block);

            var updateData = block.GetBlockUpdateData();
            //BlockDomain
            UtxoSetDac.Default.Put(updateData.NewUtxoSet);//存入新建的Utxo,暂时还是未确认

            UtxoSetDac.Default.SetSpent(updateData.SpentUtxoSet, block.Header.Height);//更新已经消费的Utxo
            UtxoStateDac.Default.Put(block.Header.Height, updateData.UtxoSetState);//设置区块影响的Utxo

            var accounts = AccountDac.Default.GetAccountBook();
            var myOutputs = updateData.NewUtxoSet.Where(x => accounts.Contains(x.Account));
            if (myOutputs.Any())
            {
                UtxoSetDac.Default.AddMyOutputs(block.Header.Height, myOutputs.Select(x => $"{x.TransactionHash}_{x.Index}").ToList());
            }

            //AppDomain
            #region 更新已确认余额/等待中的余额(暂时废弃)
            //暂时废弃，还有每次获取余额都会刷新，重复操作
            //UtxoSetDac.Default.UpdateUtxoSet(block.Header.Height);
            #endregion

            #region 删除已经消费的Utxo
            if (updateData.SpentUtxoSet.Any())
            {
                TransactionPoolDac.Default.DelByHashIndex(updateData.SpentUtxoSet);//更新已经消费的Utxo
            }
            #endregion

            if (!GlobalParameters.IsExplorer)
            {
                #region 添加交易记录（可以设置不添加）
                if (GlobalParameters.IsLoadTransRecord)
                {
                    List<PaymentCache> payments = new List<PaymentCache>();
                    var group = updateData.NewUtxoSet.GroupBy(x => x.TransactionHash);
                    foreach (var transaction in block.Transactions)
                    {
                        var newPayments = transaction.GetPayments(accounts);
                        if (newPayments.Any())
                        {
                            newPayments.ForEach(x => payments.Add(x));
                        }
                    }
                    if (payments.Any())
                    {
                        PaymentDac.Default.Insert(payments);
                    }
                }
                #endregion
            }
            else
            {
                UpdateExplorer(block, updateData);
            }
            //最先存区块，最后存Link，钱包打开时需要检验是否一致，否则需要重新写入
            var linkKey = GetKey(BlockTables.Link_Block_Height_Hash, block.Header.Height);
            BlockDomain.Put(linkKey, block.Header.Hash);
        }

        public virtual BlockMsg SelectLast()
        {
            long maxHeight = 0;
            if (!long.TryParse(BlockDomain.Get(BlockDataSetting.LatestBlockHeight), out maxHeight))
                return null;
            return SelectByHeight(maxHeight);
        }

        public virtual BlockMsg SelectLastConfirmed()
        {
            long maxHeight = 0;
            if (!long.TryParse(BlockDomain.Get(BlockDataSetting.LatestBlockHeight), out maxHeight) || maxHeight < 6)
                return null;
            return SelectByHeight(maxHeight - 6);
        }

        public virtual BlockMsg SelectByHeight(long id)
        {
            var heightKey = GetKey(BlockTables.Link_Block_Height_Hash, id.ToString());
            var hash = BlockDomain.Get(heightKey);
            
            return SelectByHash(hash);
        }

        public virtual string GetBlockHashByHeight(long height)
        {
            var heightKey = GetKey(BlockTables.Link_Block_Height_Hash, height.ToString());
            return BlockDomain.Get(heightKey);
        }

        public virtual IEnumerable<string> GetBlockHashByHeight(IEnumerable<long> heights)
        {
            var heightKeys = heights.Select(height => GetKey(BlockTables.Link_Block_Height_Hash, height.ToString()));
            return BlockDomain.Get(heightKeys);
        }

        public virtual IEnumerable<BlockMsg> SelectByHeights(IEnumerable<long> heights)
        {
            var heightsKey = heights.Select(x => GetKey(BlockTables.Link_Block_Height_Hash, x.ToString()));
            var hashes = BlockDomain.Get(heightsKey);
            return SelectByHashes(hashes);
        }

        public virtual BlockMsg SelectByHash(string hash)
        {
            var hashKey = GetKey(BlockTables.Block, hash);
            return BlockDomain.Get<BlockMsg>(hashKey);
        }

        public virtual bool BlockHashExist(string hash)
        {
            return SelectByHash(hash) != null;
        }

        public virtual IEnumerable<BlockMsg> SelectByHashes(IEnumerable<string> hashes)
        {
            var hashesKey = hashes.Select(x => GetKey(BlockTables.Block, x));
            return BlockDomain.Get<BlockMsg>(hashesKey);
        }

        public virtual IEnumerable<BlockConstraint> GetConstraintsByTxHashs(IEnumerable<string> hashs)
        {
            var hashKeys = hashs.Select(x => GetKey(BlockTables.UtxoSet, $"{x}_0"));
            var utxoSets = BlockDomain.Get<UtxoSet>(hashKeys);

            return utxoSets.Select(x => new BlockConstraint
            {
                Height = x.BlockHeight,
                IsCoinBase = x.IsCoinbase,
                LockTime = x.Locktime,
                TransactionHash = x.TransactionHash,
            });
        }

        public bool IsInitExplorerComplete = false;

        public void UpdateExplorer(BlockMsg block, BlockUpdateData updateData)
        {
            if (DataStatisticsDac.Default.Height >= block.Header.Height)
                return;

            DataStatisticsDac.Default.AddLockedUtxoSet(updateData.NewUtxoSet);
            var spents = UtxoSetDac.Default.Get(updateData.SpentUtxoSet);
            var totalIncrement = updateData.NewUtxoSet.Where(x => x.Amount > 0).Sum(x => x.Amount) - spents.Sum(x => x.Amount);
            DataStatisticsDac.Default.TotalAmount += totalIncrement;

            Dictionary<string, AmountInfo> updates = new Dictionary<string, AmountInfo>();
            foreach (var transaction in block.Transactions)
            {
                var txSpents = transaction.Inputs.Select(x => spents.FirstOrDefault(p => p.TransactionHash.Equals(x.OutputTransactionHash) && p.Index == x.OutputIndex));
                foreach (var spent in txSpents)
                {
                    if (spent == null)
                        continue;
                    if (updates.ContainsKey(spent.Account))
                    {
                        updates[spent.Account].Amount -= spent.Amount;
                        updates[spent.Account].LastUpdateTime = transaction.Timestamp;
                    }
                    else
                    {
                        var accountInfo = new AmountInfo();
                        accountInfo.Amount = -spent.Amount;
                        accountInfo.LastUpdateTime = transaction.Timestamp;

                        updates.Add(spent.Account, accountInfo);
                    }
                }

                var utxoSets = transaction.GetUtxoSets();

                foreach (var utxoSet in utxoSets)
                {
                    if (utxoSet.Amount < 0)
                        continue;
                    if (updates.ContainsKey(utxoSet.Account))
                    {
                        updates[utxoSet.Account].Amount += utxoSet.Amount;
                        updates[utxoSet.Account].LastUpdateTime = transaction.Timestamp;
                    }
                    else
                    {
                        updates.Add(utxoSet.Account, new AmountInfo { Amount = utxoSet.Amount, LastUpdateTime = transaction.Timestamp });
                    }
                }
            }

            foreach (var updateItem in updates)
            {
                DataStatisticsDac.Default.Update(updateItem.Key, updateItem.Value.Amount, updateItem.Value.LastUpdateTime);
            }

            if (IsInitExplorerComplete)
            {
                DataStatisticsDac.Default.UpdateDb(block.Header.Height);
            }
        }
    }
}
