


using OmniCoin.Consensus;
using OmniCoin.Entities;
using OmniCoin.Framework;
using OmniCoin.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Business.Extensions
{
    public struct InputExtensionParams
    {
        public string BlockHash;
        public string InputAccountId;
        public long InputAmount;
        public string TransactionHash;
    }

    public struct TransExtensionParams
    {
        public string BlockHash;
        public long Height;
        public List<Input> Inputs;
        public List<Output> Outputs;
        public long TotalInput;
        public long TotalOutput;
    }

    public static class BlockMsgExtension
    {
        public static Block ConvertToEntity(this BlockMsg blockMsg, List<Transaction> transactions)
        {
            var block = new Block();
            block.Hash = blockMsg.Header.Hash;
            block.Version = blockMsg.Header.Version;
            block.Height = blockMsg.Header.Height;
            block.PreviousBlockHash = blockMsg.Header.PreviousBlockHash;
            block.Bits = blockMsg.Header.Bits;
            block.Nonce = blockMsg.Header.Nonce;
            block.GeneratorId = blockMsg.Header.GeneratorId;
            block.Timestamp = blockMsg.Header.Timestamp;
            block.BlockSignature = blockMsg.Header.BlockSignature;
            block.PayloadHash = blockMsg.Header.PayloadHash;
            block.IsDiscarded = false;
            block.IsVerified = false;
            //交易信息
            block.Transactions = transactions;
            block.TotalAmount += transactions.Sum(x => x.TotalOutput);
            block.TotalFee += transactions.Sum(x => x.Fee);
            return block;
        }

        public static Output ConvertToEntiry(this OutputMsg outputMsg, TransactionMsg transaction, BlockMsg blockMsg)
        {
            Output output = new Output();
            output.Amount = outputMsg.Amount;
            output.BlockHash = blockMsg.Header.Hash;
            output.Index = outputMsg.Index;
            output.LockScript = outputMsg.LockScript;
            output.Size = outputMsg.Size; 
            output.Index = transaction.Outputs.IndexOf(outputMsg);
            output.TransactionHash = transaction.Hash;
            output.Spent = false;
            output.IsDiscarded = false;
            var receiverId = AccountIdHelper.CreateAccountAddressByPublicKeyHash(
                        Base16.Decode(
                            Script.GetPublicKeyHashFromLockScript(outputMsg.LockScript)
                        ));
            output.ReceiverId = receiverId;
            return output;
        }

        public static Input ConvertToEntiry(this InputMsg inputMsg, InputExtensionParams inputExtension)
        {
            Input input = new Input();
            input.AccountId = inputExtension.InputAccountId;
            input.Amount = inputExtension.InputAmount;
            input.BlockHash = inputExtension.BlockHash;
            input.IsDiscarded = false;
            input.OutputIndex = inputMsg.OutputIndex;
            input.OutputTransactionHash = inputMsg.OutputTransactionHash;
            input.Size = inputMsg.Size;
            input.TransactionHash = inputExtension.TransactionHash;
            input.UnlockScript = inputMsg.UnlockScript;
            return input;
        }

        public static Transaction ConvertToEntity(this TransactionMsg transactionMsg, TransExtensionParams transExtension, bool isCoinbase = false)
        {
            Transaction transaction = new Transaction();
            transaction.BlockHash = transExtension.BlockHash;
            transaction.ExpiredTime = transactionMsg.ExpiredTime;
            transaction.Hash = transactionMsg.Hash;
            transaction.Inputs = transExtension.Inputs;
            transaction.IsDiscarded = false;
            transaction.LockTime = transactionMsg.Locktime;
            transaction.Outputs = transExtension.Outputs;
            transaction.Size = transactionMsg.Size;
            transaction.Timestamp = transactionMsg.Timestamp;
            transaction.TotalInput = transExtension.TotalInput;
            transaction.TotalOutput = transExtension.TotalOutput;
            transaction.Version = transactionMsg.Version;
            
            if (isCoinbase)
                transaction.Fee = 0;
            else
                transaction.Fee = transExtension.TotalInput - transExtension.TotalOutput;

            return transaction;
        }
    }
}