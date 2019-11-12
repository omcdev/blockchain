


using OmniCoin.Data;
using OmniCoin.Data.Dacs;
using OmniCoin.DataAgent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Business.Extensions
{
    internal enum TxType
    {
        UnPackge,
        RepeatedCost,
        NoFoundUtxo
    }

    internal static class BlackListExtension
    {
        //public static TxType Check(TransactionPoolItem poolItem)
        //{
        //    var inputs = poolItem.Transaction.Inputs;
        //    foreach (var input in inputs)
        //    {
        //        if (!TransactionDac.Default.HasTransaction(input.OutputTransactionHash))
        //            return TxType.NoFoundUtxo;
        //        if (UtxoSetDac.Default.HasCost(input.OutputTransactionHash, input.OutputIndex))
        //            return TxType.RepeatedCost;
        //    }
        //    return TxType.UnPackge;
        //}
    }
}
