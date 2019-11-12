


using OmniCoin.Data.Entities;
using OmniCoin.Entities.Explorer;
using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Data.Dacs
{
    public class AmountInfo
    {
        public long Amount;
        public long LastUpdateTime;
    }

    public class DataStatisticsDac : ExplorerDbBase<DataStatisticsDac>
    {
        /// <summary>
        /// 所有的钱(包括锁定的)
        /// </summary>
        public long TotalAmount;
        
        public long Height { get; private set; }

        protected List<UtxoSet> lockedUtxosets = new List<UtxoSet>();
        protected Dictionary<string, AmountInfo> accountAmounts = new Dictionary<string, AmountInfo>();

        object lockobj = new object();

        public void Init()
        {
            var model = ExplorerDomain.Get<DataStatisticsModel>(ExplorerSetting.DataStatistics);
            if (model == null)
            {
                Height = -1;
                return;
            }
            TotalAmount = model.TotalAmount;
            lockedUtxosets = UtxoSetDac.Default.Get(model.lockedUtxoSets).ToList();
            accountAmounts = model.AccountsInfo;
            Height = model.BlockHeight;
        }

        /// <summary>
        /// 更新钱包余额
        /// </summary>
        /// <param name="account">地址</param>
        /// <param name="IncrementAmount">增量资金</param>
        public void Update(string account, long incrementAmount, long lasttime)
        {
            if (!accountAmounts.ContainsKey(account))
            {
                lock (lockobj)
                {
                    accountAmounts.Add(account, new AmountInfo { Amount = incrementAmount, LastUpdateTime = lasttime });
                }
            }
            else
            {
                var amount = accountAmounts[account];
                amount.Amount += incrementAmount;
                amount.LastUpdateTime = amount.LastUpdateTime > lasttime ? amount.LastUpdateTime : lasttime;
                if (amount.Amount == 0)
                {
                    lock (lockobj)
                    {
                        accountAmounts.Remove(account);
                    }
                }
                else
                {
                    lock (lockobj)
                    {
                        accountAmounts[account] = amount;
                    }
                }
            }
        }

        /// <summary>
        /// 添加锁定的UtxoSet
        /// </summary>
        /// <param name="utxoSet"></param>
        public void AddLockedUtxoSet(List<UtxoSet> utxoSets)
        {
            lock (lockobj)
            {
                lockedUtxosets.AddRange(utxoSets);
            }
        }

        public long GetAmountNoLock()
        {
            var time = Time.EpochTime;
            lock (lockobj)
            {
                lockedUtxosets.RemoveAll(x => x.Locktime > time);
            }
            var locks = lockedUtxosets.ToArray();
            if (locks.Any())
            {
                return TotalAmount - locks.Sum(x => x.Amount);
            }
            return TotalAmount;
        }

        public long GetTotalAmount()
        {
            return TotalAmount;
        }

        public List<RichAddressInfo> GetAccountDataWithPage(int skipCount, int takeCount)
        {
            var totalAmount = TotalAmount;
            var accountInfos = accountAmounts.OrderByDescending(x => x.Value.Amount).Skip(skipCount).Take(takeCount).ToList();

            if (!accountInfos.Any())
                return null;

            List<RichAddressInfo> result = new List<RichAddressInfo>();
            accountInfos.ForEach(x =>
            {
                result.Add(new RichAddressInfo
                {
                    ReceiverId = x.Key,
                    Percent = Math.Round(x.Value.Amount * 100.0 / totalAmount, 2).ToString() + "%",
                    UserAmount = x.Value.Amount,
                    Timestamp = x.Value.LastUpdateTime,
                });
            });
            return result;
        }

        public void UpdateDb(long blockHeight)
        {
            var localTime = Time.EpochTime;
            lockedUtxosets.RemoveAll(x => x.IsConfirmed(blockHeight) && x.Locktime < localTime);

            DataStatisticsModel model = new DataStatisticsModel();
            model.AccountsInfo = accountAmounts;
            model.TotalAmount = TotalAmount;
            model.lockedUtxoSets = lockedUtxosets.Select(x => $"{x.TransactionHash}_{x.Index}").ToList();
            model.BlockHeight = blockHeight;
            ExplorerDomain.Put(ExplorerSetting.DataStatistics, model);
        }

        public long GetUnSpentAmount(string account)
        {
            if (accountAmounts.ContainsKey(account))
                return accountAmounts[account].Amount;
            return 0;
        }
    }

    public class DataStatisticsModel
    {
        public long TotalAmount;
        public Dictionary<string, AmountInfo> AccountsInfo;
        public List<string> lockedUtxoSets;
        public long BlockHeight;
    }
}