


using OmniCoin.Data.Dacs;
using OmniCoin.Data.Entities;
using OmniCoin.Entities;
using OmniCoin.Entities.CacheModel;
using OmniCoin.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Data
{
    public class UtxoSetState
    {
        public string Transaction;
        public int Index;
        /// <summary>
        /// 0表示添加，1表示消费
        /// </summary>
        public int State;
    }

    public class BlockUpdateData
    {
        public BlockUpdateData()
        {
            NewUtxoSet = new List<UtxoSet>();
            SpentUtxoSet = new List<string>();
            UtxoSetState = new List<UtxoSetState>();
        }

        /// <summary>
        /// 新添加的Utxo一般都是确认次数为0的
        /// </summary>
        public List<UtxoSet> NewUtxoSet;
        /// <summary>
        /// 已花费的Utxo
        /// </summary>
        public List<string> SpentUtxoSet;
        /// <summary>
        /// 
        /// </summary>
        public List<UtxoSetState> UtxoSetState;
    }

    public static class DtoExtensions
    {
        public static Func<string, string> GetAccountByLockScript;

        /// <summary>
        /// 获取到区块受影响的UTXO信息
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public static BlockUpdateData GetBlockUpdateData(this BlockMsg block)
        {
            BlockUpdateData blockUpdateData = new BlockUpdateData();
            var coinbase = block.Transactions[0];

            foreach (var tx in block.Transactions)
            {
                foreach (var output in tx.Outputs)
                {
                    UtxoSet utxoSet = new UtxoSet
                    {
                        BlockHeight = block.Header.Height,
                        IsCoinbase = tx.Hash.Equals(coinbase.Hash),
                        IsSpent = false,
                        Locktime = tx.Locktime,
                        DepositTime = tx.DepositTime,
                        TransactionHash = tx.Hash,
                        Index = output.Index,
                        Amount = output.Amount,
                        BlockHash = block.Header.Hash,
                        BlockTime = block.Header.Timestamp,
                        TransactionTime = tx.Timestamp,
                        LockScript = output.LockScript,
                        Account = GetAccountByLockScript == null ? null : GetAccountByLockScript(output.LockScript)
                    };
                    blockUpdateData.NewUtxoSet.Add(utxoSet);
                    blockUpdateData.UtxoSetState.Add(new UtxoSetState { Transaction = tx.Hash, Index = output.Index });
                }
                foreach (var input in tx.Inputs)
                {
                    blockUpdateData.SpentUtxoSet.Add($"{input.OutputTransactionHash}_{input.OutputIndex}");
                    blockUpdateData.UtxoSetState.Add(new UtxoSetState { Transaction = input.OutputTransactionHash, Index = input.OutputIndex, State = 1 });
                }
            }
            return blockUpdateData;
        }

        public static string GetAccountByLockscript(string script)
        {
            var account = GetAccountByLockScript == null ? null : GetAccountByLockScript(script);
            return account;
        }

        public static List<UtxoSet> GetUtxoSets(this TransactionMsg transaction)
        {
            List<UtxoSet> sets = new List<UtxoSet>();
            foreach (var output in transaction.Outputs)
            {
                sets.Add(new UtxoSet
                {
                    BlockHeight = 0,
                    IsCoinbase = false,
                    IsSpent = false,
                    Locktime = transaction.Locktime,
                    TransactionHash = transaction.Hash,
                    Index = output.Index,
                    Amount = output.Amount,
                    BlockHash = null,
                    BlockTime = 0,
                    TransactionTime = transaction.Timestamp,
                    LockScript = output.LockScript,
                    Account = GetAccountByLockScript == null ? null : GetAccountByLockScript(output.LockScript)
                });
            }
            return sets;
        }

        public static List<PaymentCache> GetPayments(this TransactionMsg transaction, List<string> accounts = null)
        {
            List<PaymentCache> payments = new List<PaymentCache>();
            if (accounts == null)
                accounts = AccountDac.Default.GetAccountBook();
            var spentHashIndexs = transaction.Inputs.Select(x => $"{x.OutputTransactionHash}_{x.OutputIndex}");
            var outUtxos = UtxoSetDac.Default.Get(spentHashIndexs);
            var hasmyInput = outUtxos.Any(x => accounts.Contains(x.Account));

            var newUtxos = transaction.GetUtxoSets();
            var otherUtxos = newUtxos.Where(x => !accounts.Contains(x.Account));
            var hasOtherOuput = otherUtxos.Any();
            if (!hasmyInput && newUtxos.Count == otherUtxos.Count())
                return payments;

            if (hasmyInput)
            {
                if (transaction.InputCount == 1 && transaction.Inputs[0].OutputTransactionHash == DbDomains.EmptyHash)
                {
                    //挖矿所得
                    var myUtxo = newUtxos.FirstOrDefault();
                    PaymentCache payment = new PaymentCache();
                    payment.address = myUtxo.Account;
                    payment.account = "";
                    payment.category = PaymentCatelog.Generate;
                    payment.totalInput = 0;
                    payment.totalOutput = myUtxo.Amount;
                    payment.amount = myUtxo.Amount;
                    payment.fee = 0;
                    payment.txId = myUtxo.TransactionHash;
                    payment.vout = 0;
                    payment.time = myUtxo.TransactionTime;
                    payment.size = transaction.Outputs.FirstOrDefault().Size;

                    payments.Add(payment);
                }
                else if (!hasOtherOuput)
                {
                    //给自己
                    var totalInput = outUtxos.Sum(x => x.Amount);//总输入
                    var totalOuput = newUtxos.Sum(x => x.Amount);//总支出
                    var totalfee = totalInput - totalOuput;//手续费

                    var output = newUtxos.FirstOrDefault();
                    PaymentCache payment = new PaymentCache();
                    payment.address = output.Account;
                    payment.account = "";
                    payment.category = PaymentCatelog.Self;
                    payment.amount = output.Amount;
                    payment.fee = totalfee;
                    payment.size = transaction.Outputs.Sum(x => x.Size);
                    payment.time = output.TransactionTime;
                    payment.totalInput = totalInput;
                    payment.totalOutput = output.Amount;
                    payment.txId = output.TransactionHash;
                    payment.vout = 0;

                    payments.Add(payment);
                }
                else
                {
                    //发送
                    var totalInput = outUtxos.Sum(x => x.Amount);//总输入
                    var totalOuput = newUtxos.Sum(x => x.Amount);//总支出
                    var totalfee = totalInput - totalOuput;//手续费
                    
                    bool useFee = false;
                    foreach (var newUtxo in newUtxos)
                    {
                        if (accounts.Contains(newUtxo.Account))
                            continue;
                        PaymentCache payment = new PaymentCache();
                        payment.category = PaymentCatelog.Send;

                        payment.address = newUtxo.Account;
                        payment.account = "";
                        if (!useFee)
                        {
                            payment.fee = totalfee;
                            useFee = true;
                        }
                        payment.amount = newUtxo.Amount + payment.fee;
                        payment.size = 85;
                        payment.time = newUtxo.TransactionTime;
                        payment.totalInput = totalInput;
                        payment.totalOutput = newUtxo.Amount;
                        payment.txId = newUtxo.TransactionHash;
                        payment.vout = newUtxo.Index;

                        payments.Add(payment);
                    }
                }
            }
            else
            {
                //接收
                var totalInput = outUtxos.Sum(x => x.Amount);//总输入
                var totalOuput = newUtxos.Sum(x => x.Amount);//总支出
                var totalfee = totalInput - totalOuput;//手续费

                foreach (var newUtxo in newUtxos)
                {
                    if (!accounts.Contains(newUtxo.Account))
                        continue;
                    PaymentCache payment = new PaymentCache();
                    payment.category = PaymentCatelog.Receive;

                    payment.address = newUtxo.Account;
                    payment.account = "";
                    if (newUtxo.Index == 0)
                        payment.fee = totalfee;
                    payment.amount = newUtxo.Amount;
                    payment.size = 85;
                    payment.time = newUtxo.TransactionTime;
                    payment.totalInput = totalInput;
                    payment.totalOutput = newUtxo.Amount;
                    payment.txId = newUtxo.TransactionHash;
                    payment.vout = newUtxo.Index;

                    payments.Add(payment);
                }
            }
            return payments;
        }
    }
}
