


using OmniCoin.Consensus;
using OmniCoin.Data;
using OmniCoin.Data.Dacs;
using OmniCoin.DataAgent;
using OmniCoin.Entities;
using OmniCoin.Entities.CacheModel;
using OmniCoin.Framework;
using OmniCoin.Messages;
using System.Collections.Generic;
using System.Linq;

namespace OmniCoin.DataAgent
{
    public static class Converters
    {
        public static Transaction ConvertTxMsgToEntity(this TransactionMsg txMsg,bool isSelf = false)
        {
            var entity = new Transaction();

            entity.Hash = txMsg.Hash;
            entity.BlockHash = null;
            entity.Version = txMsg.Version;
            entity.Timestamp = txMsg.Timestamp;
            entity.LockTime = txMsg.Locktime;
            entity.Inputs = new List<Input>();
            entity.Outputs = new List<Output>();

            long totalInput = 0L;
            long totalOutput = 0L;
            
            foreach (var inputMsg in txMsg.Inputs)
            {
                var input = new Input();
                input.TransactionHash = txMsg.Hash;
                input.OutputTransactionHash = inputMsg.OutputTransactionHash;
                input.OutputIndex = inputMsg.OutputIndex;
                input.Size = inputMsg.Size;
                input.UnlockScript = inputMsg.UnlockScript;
                var utxo = UtxoSetDac.Default.Get(inputMsg.OutputTransactionHash, inputMsg.OutputIndex);
                if (utxo != null)
                {
                    input.Amount = utxo.Amount;
                    input.AccountId = utxo.Account;
                    input.BlockHash = utxo.BlockHash;
                }
                else
                {
                    input.Amount = 0;
                    input.AccountId = "";
                }
                
                entity.Inputs.Add(input);
                totalInput += input.Amount;
            }

            foreach (var outputMsg in txMsg.Outputs)
            {
                var output = new Output();
                output.Index = outputMsg.Index;
                output.TransactionHash = entity.Hash;
                var address = AccountIdHelper.CreateAccountAddressByPublicKeyHash(
                    Base16.Decode(
                        Script.GetPublicKeyHashFromLockScript(outputMsg.LockScript)
                    ));
                output.ReceiverId = address;
                output.Amount = outputMsg.Amount;
                output.Size = outputMsg.Size;
                output.LockScript = outputMsg.LockScript;
                entity.Outputs.Add(output);
                totalOutput += output.Amount;
            }

            entity.TotalInput = totalInput;
            entity.TotalOutput = totalOutput;
            entity.Fee = totalInput - totalOutput;
            entity.Size = txMsg.Size;

            var coinbaseHashIndex = $"{txMsg.Hash}_{0}";
            var utxoset = UtxoSetDac.Default.Get(coinbaseHashIndex);
            entity.BlockHash = utxoset?.BlockHash;

            if (txMsg.Inputs.Count == 1 &&
                txMsg.Outputs.Count == 1 &&
                txMsg.Inputs[0].OutputTransactionHash == Base16.Encode(HashHelper.EmptyHash()))
            {
                entity.Fee = 0;
            }

            return entity;
        }
    }
}