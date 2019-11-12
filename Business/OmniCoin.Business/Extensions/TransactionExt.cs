


using OmniCoin.Data;
using OmniCoin.Data.Dacs;
using OmniCoin.Entities;
using OmniCoin.Entities.Explorer;
using OmniCoin.Framework;
using OmniCoin.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Business.Extensions
{
    public static class TransactionExt
    {
        public static TransOM ConvertToOM(this TransactionMsg transaction)
        {
            TransOM result = new TransOM();
            result.Hash = transaction.Hash;
            result.Size = transaction.Size;
            result.Timestamp = transaction.Timestamp;

            var firstUtxo = UtxoSetDac.Default.Get(transaction.Hash, 0);

            result.OutputAffirm = GlobalParameters.LocalHeight - firstUtxo.BlockHeight;
            result.OutputAmount = transaction.Outputs.Sum(x => x.Amount);

            result.InputList = new List<InputOM>();
            if (transaction.InputCount == 1 && transaction.Inputs[0].OutputTransactionHash.Equals(DbDomains.EmptyHash))
            {
                result.InputList.Add(new InputOM
                {
                    AccountId = "Coinbase",
                    Amount = 0,
                    OutputTransactionHash = DbDomains.EmptyHash,
                    UnlockScript = transaction.Inputs[0].UnlockScript
                });
            }
            else
            {
                var spentHashIndexs = transaction.Inputs.Select(x => $"{x.OutputTransactionHash}_{x.OutputIndex}");
                var utxos = UtxoSetDac.Default.Get(spentHashIndexs);

                transaction.Inputs.ForEach(x =>
                {
                    var utxo = utxos.FirstOrDefault(p => p.Index == x.OutputIndex && p.TransactionHash == x.OutputTransactionHash);
                    if (utxo != null)
                    {
                        InputOM input = new InputOM();
                        input.AccountId = utxo.Account;
                        input.Amount = utxo.Amount;
                        input.OutputTransactionHash = utxo.TransactionHash;
                        input.UnlockScript = utxo.LockScript;
                        result.InputList.Add(input);
                    }
                });
            }

            result.OutputList = new List<OutputOM>();
            var newUtxos = transaction.GetUtxoSets();
            newUtxos.ForEach(x =>
            {
                OutputOM output = new OutputOM();
                output.Amount = x.Amount;
                output.LockScript = x.LockScript;
                output.ReceiverId = x.Account;
                output.Spent = UtxoSetDac.Default.Get(x.TransactionHash, x.Index).IsSpent;
                result.OutputList.Add(output);
            });
            return result;
        }

        public static TransactionDetail ConvertToDetail(this TransactionMsg transaction)
        {
            TransactionDetail result = new TransactionDetail();
            result.Hash = transaction.Hash;
            result.Size = transaction.Size;
            result.Timestamp = transaction.Timestamp;

            var firstUtxo = UtxoSetDac.Default.Get(transaction.Hash, 0);

            result.OutputAffirm = GlobalParameters.LocalHeight - firstUtxo.BlockHeight;
            result.OutputAmount = transaction.Outputs.Sum(x => x.Amount);
            result.BlockHash = firstUtxo.BlockHash;
            result.BlockHeight = firstUtxo.BlockHeight;
            result.LockTime = transaction.Locktime;
            result.TotalInput = transaction.InputCount;
            result.TotalOutput = transaction.OutputCount;

            result.InputList = new List<InputOM>();
            if (transaction.InputCount == 1 && transaction.Inputs[0].OutputTransactionHash.Equals(DbDomains.EmptyHash))
            {
                result.Fee = 0;
                result.InputList.Add(new InputOM
                {
                    AccountId = "Coinbase",
                    Amount = 0,
                    OutputTransactionHash = DbDomains.EmptyHash,
                    UnlockScript = transaction.Inputs[0].UnlockScript
                });
            }
            else
            {
                var spentHashIndexs = transaction.Inputs.Select(x => $"{x.OutputTransactionHash}_{x.OutputIndex}");
                var utxos = UtxoSetDac.Default.Get(spentHashIndexs);
                result.InputAmount = utxos.Sum(x => x.Amount);
                result.Fee = result.InputAmount - result.OutputAmount;

                transaction.Inputs.ForEach(x =>
                {
                    var utxo = utxos.FirstOrDefault(p => p.Index == x.OutputIndex && p.TransactionHash == x.OutputTransactionHash);
                    if (utxo != null)
                    {
                        InputOM input = new InputOM();
                        input.AccountId = utxo.Account;
                        input.Amount = utxo.Amount;
                        input.OutputTransactionHash = utxo.TransactionHash;
                        input.UnlockScript = utxo.LockScript;
                        result.InputList.Add(input);
                    }
                });
            }

            result.OutputList = new List<OutputOM>();
            var newUtxos = transaction.GetUtxoSets();
            newUtxos.ForEach(x =>
            {
                OutputOM output = new OutputOM();
                output.Amount = x.Amount;
                output.LockScript = x.LockScript;
                output.ReceiverId = x.Account;
                output.Spent = UtxoSetDac.Default.Get(x.TransactionHash, x.Index).IsSpent;
                result.OutputList.Add(output);
            });
            return result;
        }

        public static bool HasAddress(this TransactionMsg transaction, string account)
        {
            var newUtxos = transaction.GetUtxoSets();
            if (newUtxos.Any(x => x.Account.Equals(account)))
                return true;

            var spentHashIndexs = transaction.Inputs.Select(x => $"{x.OutputTransactionHash}_{x.OutputIndex}");

            foreach (var input in transaction.Inputs)
            {
                var utxo = UtxoSetDac.Default.Get(input.OutputTransactionHash, input.OutputIndex);
                if (utxo != null && utxo.Account.Equals(account))
                    return true;
            }
            return false;
        }
    }
}
