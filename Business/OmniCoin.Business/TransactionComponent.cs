

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Business.Extensions;
using OmniCoin.Business.ParamsModel;
using OmniCoin.Consensus;
using OmniCoin.Data.Dacs;
using OmniCoin.DataAgent;
using OmniCoin.DTO;
using OmniCoin.Entities;
using OmniCoin.Framework;
using OmniCoin.Messages;
using OmniCoin.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using OmniCoin.Data.Entities;
using System.Diagnostics;
using OmniCoin.Entities.Explorer;

namespace OmniCoin.Business
{
    public class TransactionComponent
    {
        public void CreateNewTransaction(TransactionMsg transaction, long feeRate)
        {
            var spents = transaction.Inputs.Select(x => $"{x.OutputTransactionHash}_{x.OutputIndex}");
            var spentUtxos = UtxoSetDac.Default.Get(spents);
            if (spents.Count() == spentUtxos.Count() && !spentUtxos.Any(x => !x.IsConfirmed(GlobalParameters.LocalHeight)))
                TransactionPool.Instance.AddNewTransaction(feeRate, transaction);
            else
                TransactionPool.Instance.AddTxNoOutput(transaction);
        }

        public void AddTransactionToPool(TransactionMsg transaction)
        {
            var isBlacked = BlacklistTxs.Current.IsBlacked(transaction.Hash);
            if (isBlacked)
                return;

            if (TransactionPoolDac.Default.IsExist(transaction.Hash))
                return;

            if (TransactionDac.Default.HasTransaction(transaction.Hash))
                return;

            long feeRate = 0;
            long totalInput = 0;
            long totalOutput = 0;
            long fee = 0;
            try
            {
                var result = VerifyTransaction(transaction, out fee, out totalOutput, out totalInput);
                if (!result)
                {
                    TransactionPool.Instance.AddTxNoOutput(transaction);
                    return;
                }
                else
                {
                    CreateNewTransaction(transaction, feeRate);
                }
            }
            catch (CommonException ex)
            {
                LogHelper.Error(transaction.Hash + ":" + ex.ToString());
                //交易出错时，需要添加到黑名单所在的文件夹
                BlacklistTxs.Current.Add(transaction.Hash);
                BlacklistTxs.Current.AddToBlackFile(transaction);
                throw ex;
            }
        }

        public bool VerifyTransaction(TransactionMsg transaction, out long txFee, out long totalOutput, out long totalInput)
        {
            var blockComponent = new BlockComponent();
            txFee = 0;
            //compatible with old node
            if (transaction.Locktime > 0 && transaction.ExpiredTime == transaction.Locktime)
            {
                transaction.ExpiredTime = 0;
            }

            //step 0
            if (transaction.Hash != transaction.GetHash())
            {
                LogHelper.Error("Tx Hash Error:" + transaction.Hash);
                LogHelper.Error("Timestamp:" + transaction.Timestamp);
                LogHelper.Error("Locktime:" + transaction.Locktime);
                LogHelper.Error("ExpiredTime:" + transaction.ExpiredTime);
                LogHelper.Error("InputCount:" + transaction.InputCount);
                LogHelper.Error("OutputCount:" + transaction.OutputCount);

                throw new CommonException(ErrorCode.Engine.Transaction.Verify.TRANSACTION_HASH_ERROR);
            }

            //step 1
            if (transaction.InputCount == 0 || transaction.OutputCount == 0)
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.INPUT_AND_OUTPUT_CANNOT_BE_EMPTY);
            }

            //step 2
            if (transaction.Hash == Base16.Encode(HashHelper.EmptyHash()))
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.HASH_CANNOT_BE_EMPTY);
            }

            //step 3
            if (transaction.Locktime < 0 || transaction.Locktime > (Time.EpochTime + BlockSetting.LOCK_TIME_MAX))
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.LOCK_TIME_EXCEEDED_THE_LIMIT);
            }

            //step 4
            if (transaction.Serialize().Length < BlockSetting.TRANSACTION_MIN_SIZE)
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.TRANSACTION_SIZE_BELOW_THE_LIMIT);
            }

            //step 5
            if (this.existsInDB(transaction.Hash))
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.TRANSACTION_HAS_BEEN_EXISTED);
            }

            totalOutput = 0;
            totalInput = 0;

            foreach (var output in transaction.Outputs)
            {
                if (output.Amount <= 0 || output.Amount > BlockSetting.OUTPUT_AMOUNT_MAX)
                {
                    throw new CommonException(ErrorCode.Engine.Transaction.Verify.OUTPUT_EXCEEDED_THE_LIMIT);
                }

                if (!Script.VerifyLockScriptFormat(output.LockScript))
                {
                    throw new CommonException(ErrorCode.Engine.Transaction.Verify.SCRIPT_FORMAT_ERROR);
                }

                totalOutput += output.Amount;
            }

            var count = transaction.Inputs.Distinct().Count();
            if (count != transaction.Inputs.Count)
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.UTXO_DUPLICATED_IN_ONE_TRANSACTION);
            }

            foreach (var input in transaction.Inputs)
            {
                if (!Script.VerifyUnlockScriptFormat(input.UnlockScript))
                {
                    throw new CommonException(ErrorCode.Engine.Transaction.Verify.SCRIPT_FORMAT_ERROR);
                }

                var utxo = UtxoSetDac.Default.Get(input.OutputTransactionHash, input.OutputIndex);
                if (utxo != null)
                {
                    if (!utxo.IsConfirmed(GlobalParameters.LocalHeight))
                    {
                        if (utxo.IsCoinbase)
                            throw new CommonException(ErrorCode.Engine.Transaction.Verify.COINBASE_NEED_100_CONFIRMS);
                        else
                            return false;
                    }

                    if (Time.EpochTime < utxo.Locktime)
                    {
                        throw new CommonException(ErrorCode.Engine.Transaction.Verify.TRANSACTION_IS_LOCKED);
                    }

                    if (utxo.IsSpent())
                    {
                        throw new CommonException(ErrorCode.Engine.Transaction.Verify.UTXO_HAS_BEEN_SPENT);
                    }

                    if (!Script.VerifyLockScriptByUnlockScript(input.OutputTransactionHash, input.OutputIndex, utxo.LockScript, input.UnlockScript))
                    {
                        throw new CommonException(ErrorCode.Engine.Transaction.Verify.UTXO_UNLOCK_FAIL);
                    }

                    totalInput += utxo.Amount;
                }
                else
                {
                    //not found output, wait for other transactions or blocks;
                    txFee = 0;
                    return false;
                }
            }

            if (totalOutput >= totalInput)
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.OUTPUT_LARGE_THAN_INPUT);
            }

            if ((totalInput - totalOutput) < BlockSetting.TRANSACTION_MIN_FEE)
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.TRANSACTION_FEE_IS_TOO_FEW);
            }

            if (totalInput > BlockSetting.INPUT_AMOUNT_MAX)
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.INPUT_EXCEEDED_THE_LIMIT);
            }

            if (totalOutput > BlockSetting.OUTPUT_AMOUNT_MAX)
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.OUTPUT_EXCEEDED_THE_LIMIT);
            }

            txFee = totalInput - totalOutput;
            return true;
        }

        public bool VerifyTransactionMsg(VerifyTransactionModel model, out long fee)
        {
            var transaction = model.transaction;

            //校验锁定时间
            if (transaction.Locktime > 0 && transaction.ExpiredTime == transaction.Locktime)
            {
                transaction.ExpiredTime = 0;
            }

            //校验HASH值
            if (transaction.Hash != transaction.GetHash())
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.TRANSACTION_HASH_ERROR);
            }

            //交易必须包含输入和输出
            if (transaction.InputCount == 0 || transaction.OutputCount == 0)
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.INPUT_AND_OUTPUT_CANNOT_BE_EMPTY);
            }

            //校验交易的时间
            if (transaction.Locktime < 0 || transaction.Locktime > (Time.EpochTime + BlockSetting.LOCK_TIME_MAX))
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.LOCK_TIME_EXCEEDED_THE_LIMIT);
            }

            //校验交易量
            if (transaction.Serialize().Length < BlockSetting.TRANSACTION_MIN_SIZE)
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.TRANSACTION_SIZE_BELOW_THE_LIMIT);
            }

            return VerifyTransactionData(model, out fee);
        }

        readonly long[] Heights = new long[] { 19288, 19306, 19314, 19329 };
        public const string COINBASE_INPUT_HASH = "0000000000000000000000000000000000000000000000000000000000000000";

        public bool VerifyTransactionData(VerifyTransactionModel model, out long fee)
        {
            long totalOutput = 0;
            long totalInput = 0;

            var transaction = model.transaction;
            var block = model.block;
            var localHeight = model.localHeight;

            List<Output> outputEntities = new List<Output>();
            List<Input> inputEntities = new List<Input>();

            #region 校验和转换 接收地址
            foreach (var output in transaction.Outputs)
            {
                if ((output.Amount <= 0 || output.Amount > BlockSetting.OUTPUT_AMOUNT_MAX) && !Heights.Contains(block.Header.Height))
                {
                    throw new CommonException(ErrorCode.Engine.Transaction.Verify.OUTPUT_EXCEEDED_THE_LIMIT);
                }

                if (!Script.VerifyLockScriptFormat(output.LockScript))
                {
                    throw new CommonException(ErrorCode.Engine.Transaction.Verify.SCRIPT_FORMAT_ERROR);
                }

                var outputEntity = output.ConvertToEntiry(transaction, block);
                outputEntities.Add(outputEntity);

                totalOutput += output.Amount;
            }
            #endregion

            #region 判断是不是在一个区块内重复花费（24696之前有错误数据）
            if (block.Header.Height <= 24696)
            {
                var inputsCount = transaction.Inputs.Select(x => x.OutputTransactionHash + x.OutputIndex).Distinct().Count();
                if (inputsCount < transaction.Inputs.Count)
                {
                    throw new CommonException(ErrorCode.Engine.Transaction.Verify.UTXO_NOT_EXISTED);
                }
            }
            #endregion


            var firstInput = transaction.Inputs[0];
            bool isCoinbase = firstInput.OutputTransactionHash == COINBASE_INPUT_HASH;
            if (isCoinbase)
            {
                #region 如果是Coinbase
                var inputps = new InputExtensionParams();
                inputps.BlockHash = block.Header.Hash;
                inputps.InputAccountId = null;
                inputps.InputAmount = 0;
                inputps.TransactionHash = transaction.Hash;
                var inputEntity = firstInput.ConvertToEntiry(inputps);

                totalInput += 0;
                inputEntities.Add(inputEntity);
                isCoinbase = true;
                #endregion
            }
            else
            {
                foreach (var input in transaction.Inputs)
                {
                    if (!Script.VerifyUnlockScriptFormat(input.UnlockScript))
                    {
                        throw new CommonException(ErrorCode.Engine.Transaction.Verify.SCRIPT_FORMAT_ERROR);
                    }

                    var output = UtxoSetDac.Default.Get(input.OutputTransactionHash, input.OutputIndex);
                    if (output == null)
                    {
                        throw new CommonException(ErrorCode.Engine.Transaction.Verify.UTXO_NOT_EXISTED);
                    }

                    //是否已经上链了(存在重复写入的情况)
                    if (output.IsSpent() && output.SpentHeight != block.Header.Height && block.Header.Height > 24696)
                    {
                        LogHelper.Warn($"transaction.Hash:{transaction.Hash}");
                        throw new CommonException(ErrorCode.Engine.Transaction.Verify.UTXO_HAS_BEEN_SPENT);
                    }
                    
                    long blockHeight = output.BlockHeight;
                    //判断挖矿的区块等待100个确认
                    if (output.IsCoinbase && !output.IsConfirmed(block.Header.Height))
                    {
                        if(output.IsCoinbase)
                            throw new CommonException(ErrorCode.Engine.Transaction.Verify.COINBASE_NEED_100_CONFIRMS);
                        else
                            throw new CommonException(ErrorCode.Engine.Transaction.Verify.UTXO_NEED_6_CONFIRMS);
                    }
                    //判断余额是否已经解锁
                    if (Time.EpochTime < output.Locktime)
                    {
                        throw new CommonException(ErrorCode.Engine.Transaction.Verify.TRANSACTION_IS_LOCKED);
                    }

                    //校验存币时间，当前时间大于存币过期时间才能使用
                    if (output.DepositTime > 0 && output.DepositTime >= Time.EpochTime)
                    {
                        throw new CommonException(ErrorCode.Engine.Transaction.Verify.DEPOSIT_TIME_NOT_EXPIRED);
                    }

                    string lockScript = output.LockScript;

                    if (!Script.VerifyLockScriptByUnlockScript(input.OutputTransactionHash, input.OutputIndex, lockScript, input.UnlockScript))
                    {
                        throw new CommonException(ErrorCode.Engine.Transaction.Verify.UTXO_UNLOCK_FAIL);
                    }
                    var inputps = new InputExtensionParams();
                    inputps.BlockHash = block.Header.Hash;
                    inputps.InputAccountId = output.Account;
                    inputps.InputAmount = output.Amount;
                    inputps.TransactionHash = transaction.Hash;
                    var inputEntity = input.ConvertToEntiry(inputps);

                    totalInput += output.Amount;

                    inputEntities.Add(inputEntity);
                }

                if (totalOutput >= totalInput)
                {
                    throw new CommonException(ErrorCode.Engine.Transaction.Verify.OUTPUT_LARGE_THAN_INPUT);
                }

                if ((totalInput - totalOutput) < BlockSetting.TRANSACTION_MIN_FEE)
                {
                    throw new CommonException(ErrorCode.Engine.Transaction.Verify.TRANSACTION_FEE_IS_TOO_FEW);
                }
            }

            if (totalInput > BlockSetting.INPUT_AMOUNT_MAX)
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.INPUT_EXCEEDED_THE_LIMIT);
            }

            if (totalOutput > BlockSetting.OUTPUT_AMOUNT_MAX)
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.OUTPUT_EXCEEDED_THE_LIMIT);
            }

            if (isCoinbase)
                fee = 0;
            else
                fee = totalInput - totalOutput;
            return true;
        }

        public List<string> GetAllHashesFromPool()
        {
            return TransactionPool.Instance.GetAllTransactionHashes();
        }

        public List<string> GetAllHashesRelevantWithCurrentWalletFromPool()
        {
            return TransactionPoolDac.Default.GetAllHashes();
        }

        public bool CheckTxExisted(string txHash, bool checkPool = true)
        {
            var dac = TransactionDac.Default;

            if (checkPool)
            {
                var result = TransactionPoolDac.Default.IsExist(txHash);
                if (result)
                {
                    return true;
                }
            }
            return TransactionDac.Default.HasTransaction(txHash);
        }

        public bool CheckBlackTxExisted(string txHash)
        {
            return BlacklistTxs.Current.IsBlacked(txHash);
        }

        public TransactionMsg GetTransactionMsgByHash(string txHash)
        {
            var entity = TransactionDac.Default.GetTransaction(txHash);

            if (entity != null)
            {
                return entity;
            }
            else
            {
                return TransactionPool.Instance.GetTransactionByHash(txHash);
            }
        }

        public GetTxOutOM GetTxOut(string txid, int vount, bool unconfirmed = false)
        {
            GetTxOutOM result = null;
            TransactionMsg tx = null;
            tx = TransactionDac.Default.GetTransaction(txid);
            if (tx == null && unconfirmed)
                tx = TransactionPoolDac.Default.Get(txid);

            if (tx == null)
                return result;

            if (!unconfirmed)
            {
                var utxo = UtxoSetDac.Default.Get(txid, vount);
                if (utxo == null)
                    return result;
                result = new GetTxOutOM();
                result.bestblock = utxo.BlockHash;
                result.coinbase = utxo.IsCoinbase;
                result.confirmations = GlobalParameters.LocalHeight - utxo.BlockHeight;
                result.scriptPubKey = utxo.LockScript;
                result.value = utxo.Amount;
            }
            else
            {
                var output = tx.Outputs[vount];
                result.bestblock = null;
                result.coinbase = false;
                result.confirmations = 0;
                result.scriptPubKey = output.LockScript;
                result.value = output.Amount;
            }
            result.version = Versions.EngineVersion;
            return result;
        }

        public GetTxOutSetInfoOM GetTotalBalance()
        {
            GetTxOutSetInfoOM result = new GetTxOutSetInfoOM();
            result.total_amount = UtxoSetDac.Default.GetConfirmedAmount();
            result.height = GlobalParameters.LocalHeight;
            result.bestblock = BlockDac.Default.GetBlockHashByHeight(result.height);
            return result;
        }

        public GetTxOutSetInfoOM GetTxOutSetInfo()
        {
            GetTxOutSetInfoOM result = new GetTxOutSetInfoOM();
            var blockComponent = new BlockComponent();
            result.height = GlobalParameters.LocalHeight;
            result.bestblock = BlockDac.Default.GetBlockHashByHeight(result.height);
            var accounts = AccountDac.Default.GetMyAccountBook();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var confirmedUtxos = UtxoSetDac.Default.GetMyUnspentUtxoKeys().ToArray();
            stopwatch.Stop();
            LogHelper.Warn($"[accounts Count] :: {accounts.Count}");
            LogHelper.Warn($"[GetMyUnspentUtxoKeys] use time :: {stopwatch.ElapsedMilliseconds}");
            var transacionCount = confirmedUtxos.Select(x => x.Split("_")[0]).Distinct().Count();
            result.transactions = transacionCount;
            result.txouts = confirmedUtxos.Count();
            result.total_amount = UtxoSetDac.Default.GetConfirmedAmount() - TransactionPoolDac.Default.UseBalanceInPool;
            return result;
        }
        
        public long GetUnconfimedAmount()
        {
            var total_amount = UtxoSetDac.Default.GetTotalUnConfirmedAmount();
            total_amount += TransactionPoolDac.Default.AddBalanceInPool;
            return total_amount;
        }

        public TransactionMsg GetTransactionMsgFromPool(string txHash)
        {
            return TransactionPool.Instance.GetTransactionByHash(txHash);
        }

        public UtxoSet GetUtxoSetByIndexAndTxHash(string txHash, int index)
        {
            var utxoSet = UtxoSetDac.Default.Get(txHash, index);
            return utxoSet;
        }

        public TransactionMsg GetTransactionEntityByHash(string hash)
        {
            var item = TransactionDac.Default.GetTransaction(hash);

            if (item != null)
            {
                return item;
            }
            else
            {
                var msg = TransactionPool.Instance.GetTransactionByHash(hash);
            }

            return item;
        }

        public void AddBlockedTxHash(string txHash)
        {
            BlacklistTxs.Current.Add(txHash);
        }

        //check whether output is existed. get amount and lockscript from output.
        private bool getOutput(string transactionHash, int outputIndex, out long outputAmount, out string lockScript, out long blockHeight)
        {
            var utxoset = UtxoSetDac.Default.Get(transactionHash, outputIndex);

            if (utxoset == null)
            {
                outputAmount = 0;
                lockScript = null;
                blockHeight = -1;
                return false;
            }
            else
            {
                outputAmount = utxoset.Amount;
                lockScript = utxoset.LockScript;
                blockHeight = utxoset.BlockHeight;
                return true;
            }
        }

        /// <summary>
        /// DaemonToolController专用 判断交易是否合法
        /// </summary>
        /// <param name="outputTxHash"></param>
        /// <param name="outputIndex"></param>
        /// <returns></returns>
        public bool IsTransactionValid(Dictionary<string, int> dic)
        {
            foreach(KeyValuePair<string, int> item in dic)
            {
                var set = UtxoSetDac.Default.Get(item.Key, item.Value);
                bool result = set == null ? false : set.IsSpent();
                if(result)
                {
                    return false;
                }
            }
            return true;
        }

        //Check whether output has been spent or contained in another transaction, true = spent, false = unspent
        private bool checkOutputSpent(string currentTxHash, string outputTxHash, int outputIndex, string blockHash = null)
        {
            var set = UtxoSetDac.Default.Get(outputTxHash, outputIndex);
            return set == null ? false : set.IsSpent();
        }

        private bool existsInDB(string transactionHash)
        {
            return TransactionDac.Default.HasTransaction(transactionHash);
        }

        private bool existsInTransactionPool(string transactionHash)
        {
            if (TransactionPool.Instance.GetTransactionByHash(transactionHash) != null)
            {
                return true;
            }

            return false;
        }

        public TransactionDetail GetTransactionDetail(string txHash)
        {
            if (!TransactionDac.Default.HasTransaction(txHash))
                return null;

            var transaction = TransactionDac.Default.GetTransaction(txHash);
            return transaction.ConvertToDetail();
        }
    }
}