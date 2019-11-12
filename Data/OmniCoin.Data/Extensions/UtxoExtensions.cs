


using OmniCoin.Data.Dacs;
using OmniCoin.Data.Entities;
using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Data
{
    public static class UtxoExtensions
    {
        /// <summary>
        /// 是否已确认
        /// </summary>
        /// <param name="utxoSet"></param>
        /// <param name="lastHeight"></param>
        /// <returns></returns>
        public static bool IsConfirmed(this UtxoSet utxoSet, long lastHeight)
        {
            var confirmcount = lastHeight - utxoSet.BlockHeight;
            return (confirmcount >= 100 && utxoSet.IsCoinbase) || (confirmcount >= 6 && !utxoSet.IsCoinbase);
        }
        /// <summary>
        /// 是否可花费
        /// </summary>
        /// <param name="utxoSet"></param>
        /// <param name="lastHeight"></param>
        /// <param name="localTime"></param>
        /// <returns></returns>
        public static bool IsSpentable(this UtxoSet utxoSet, long lastHeight, long localTime)
        {
            if (utxoSet.IsSpent())
                return false;
            var confirmcount = lastHeight - utxoSet.BlockHeight;
            if (TransactionPoolDac.Default.UtxoHasSpent(utxoSet.TransactionHash, utxoSet.Index))
                return false;
            return (confirmcount >= 100 && utxoSet.IsCoinbase) || (confirmcount >= 6 && !utxoSet.IsCoinbase && utxoSet.Locktime <= localTime);
        }
        /// <summary>
        /// 是否可花费(排除交易池中的数据)
        /// </summary>
        /// <param name="utxoSet"></param>
        /// <param name="lastHeight"></param>
        /// <param name="localTime"></param>
        /// <returns></returns>
        private static bool IsSpentableWithoutPool(this UtxoSet utxoSet, long lastHeight, long localTime)
        {
            var confirmcount = lastHeight - utxoSet.BlockHeight;
            return !((confirmcount < 100 && utxoSet.IsCoinbase) || (confirmcount < 6 && !utxoSet.IsCoinbase) || utxoSet.Locktime > localTime);
        }
        /// <summary>
        /// 是否正在等待中
        /// </summary>
        /// <param name="utxoSet"></param>
        /// <param name="lastHeight"></param>
        /// <param name="localTime"></param>
        /// <returns></returns>
        public static bool IsWaiting(this UtxoSet utxoSet, long lastHeight, long localTime)
        {
            return !utxoSet.IsSpent() && utxoSet.IsSpentableWithoutPool(lastHeight, localTime);
        }

        /// <summary>
        /// 这笔Utxo消费是否已确认
        /// </summary>
        /// <param name="utxoSet"></param>
        /// <param name="lastHeight"></param>
        /// <returns></returns>
        public static bool IsSpentConfirmed(this UtxoSet utxoSet, long lastHeight)
        {
            var confirmcount = utxoSet.SpentHeight - lastHeight;
            return confirmcount >= 6;
        }

        public static bool IsSpent(this UtxoSet utxoSet)
        {
            return utxoSet.IsSpent && utxoSet.SpentHeight != 0 && GlobalParameters.LocalHeight >= utxoSet.SpentHeight;
        }

        public static bool IsSpentInPool(this UtxoSet utxoSet)
        {
            return utxoSet.IsSpent || TransactionPoolDac.Default.UtxoHasSpent(utxoSet.TransactionHash, utxoSet.Index);
        }
    }
}