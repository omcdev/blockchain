// Copyright (c) 2018 FiiiLab Technology Ltd
// Distributed under the MIT software license, see the accompanying
// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using FiiiChain.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace FiiiChain.DataAgent
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
}
