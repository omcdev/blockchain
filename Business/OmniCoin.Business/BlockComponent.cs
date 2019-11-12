

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Business.Extensions;
using OmniCoin.Business.ParamsModel;
using OmniCoin.Consensus;
using OmniCoin.Data;
using OmniCoin.Data.Dacs;
using OmniCoin.Data.Entities;
using OmniCoin.DataAgent;
using OmniCoin.Entities;
using OmniCoin.Entities.Explorer;
using OmniCoin.Framework;
using OmniCoin.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace OmniCoin.Business
{
    public class BlockComponent
    {
        /// <summary>
        /// 创建新的区块
        /// </summary>
        /// <param name="minerName"></param>
        /// <param name="generatorId"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public BlockMsg CreateNewBlock(string minerName, string generatorId, string remark = null, string accountId = null)
        {
            var accountDac = AccountDac.Default;
            var blockDac = BlockDac.Default;
            var txDac = TransactionDac.Default;
            var txPool = TransactionPool.Instance;
            var txComponent = new TransactionComponent();
            var transactionMsgs = new List<TransactionMsg>();

            long lastBlockHeight = -1;
            string lastBlockHash = Base16.Encode(HashHelper.EmptyHash());
            long lastBlockBits = -1;
            string lastBlockGenerator = null;

            //获取最后一个区块
            var blockEntity = blockDac.SelectLast();

            if (blockEntity != null)
            {
                lastBlockHeight = blockEntity.Header.Height;
                lastBlockHash = blockEntity.Header.Hash;
                lastBlockBits = blockEntity.Header.Bits;
                lastBlockGenerator = blockEntity.Header.GeneratorId;
            }

            long totalSize = 0;
            long totalInput = 0;
            long totalOutput = 0;
            long totalAmount = 0;
            long totalFee = 0;

            long maxSize = Consensus.BlockSetting.MAX_BLOCK_SIZE - (1 * 1024);

            //获取待打包的交易
            var txs = txPool.GetTxsWithoutRepeatCost(10, maxSize);

            var hashIndexs = new List<string>();
            foreach (var tx in txs)
            {
                totalSize += tx.Size;
                totalInput += tx.InputCount;
                totalOutput += tx.OutputCount;
                hashIndexs.AddRange(tx.Inputs.Select(x => $"{x.OutputTransactionHash}_{x.OutputIndex}"));
                long totalOutputAmount = tx.Outputs.Sum(x => x.Amount);
                totalAmount += totalOutputAmount;
            }
            var utxos = UtxoSetDac.Default.Get(hashIndexs);
            var totalInputAmount = utxos.Sum(x => x.Amount);
            totalFee = totalInputAmount - totalAmount;

            transactionMsgs.AddRange(txs);

            var accounts = AccountDac.Default.SelectAll();
            var minerAccount = accounts.OrderBy(x => x.Timestamp).FirstOrDefault();

            if (accountId != null)
            {
                var account = accounts.FirstOrDefault(x => x.Id == accountId);

                if (account != null && !string.IsNullOrWhiteSpace(account.PrivateKey))
                {
                    minerAccount = account;
                }
            }

            var minerAccountId = minerAccount.Id;
            BlockMsg newBlockMsg = new BlockMsg();
            BlockHeaderMsg headerMsg = new BlockHeaderMsg();
            headerMsg.Hash = Base16.Encode(HashHelper.EmptyHash());
            headerMsg.GeneratorId = generatorId;
            newBlockMsg.Header = headerMsg;
            headerMsg.Height = lastBlockHeight + 1;
            headerMsg.PreviousBlockHash = lastBlockHash;

            if (headerMsg.Height == 0)
            {
                minerAccountId = Consensus.BlockSetting.GenesisBlockReceiver;
                remark = Consensus.BlockSetting.GenesisBlockRemark;
            }

            BlockMsg prevBlockMsg = null;
            BlockMsg prevStepBlockMsg = null;

            if (blockEntity != null)
            {
                prevBlockMsg = blockEntity;
            }

            if (headerMsg.Height >= POC.DIFFIUCLTY_ADJUST_STEP)
            {
                var prevStepHeight = 0L;
                if (!GlobalParameters.IsTestnet && headerMsg.Height <= POC.DIFFICULTY_CALCULATE_LOGIC_ADJUST_HEIGHT)
                {
                    prevStepHeight = headerMsg.Height - POC.DIFFIUCLTY_ADJUST_STEP - 1;
                }
                else
                {
                    prevStepHeight = headerMsg.Height - POC.DIFFIUCLTY_ADJUST_STEP;
                }
                prevStepBlockMsg = blockDac.SelectByHeight(prevStepHeight);
            }

            var newBlockReward = POC.GetNewBlockReward(headerMsg.Height);

            headerMsg.Bits = POC.CalculateBaseTarget(headerMsg.Height, prevBlockMsg, prevStepBlockMsg);
            headerMsg.TotalTransaction = transactionMsgs.Count + 1;

            var coinbaseTxMsg = new TransactionMsg();
            coinbaseTxMsg.Timestamp = Time.EpochTime;
            coinbaseTxMsg.Locktime = 0;

            var coinbaseInputMsg = new InputMsg();
            coinbaseTxMsg.Inputs.Add(coinbaseInputMsg);
            coinbaseInputMsg.OutputIndex = 0;
            coinbaseInputMsg.OutputTransactionHash = Base16.Encode(HashHelper.EmptyHash());
            coinbaseInputMsg.UnlockScript = Script.BuildMinerScript(minerName, remark);
            coinbaseInputMsg.Size = coinbaseInputMsg.UnlockScript.Length;

            var coinbaseOutputMsg = new OutputMsg();
            coinbaseTxMsg.Outputs.Add(coinbaseOutputMsg);
            coinbaseOutputMsg.Amount = newBlockReward + totalFee;
            coinbaseOutputMsg.LockScript = Script.BuildLockScipt(minerAccountId);
            coinbaseOutputMsg.Size = coinbaseOutputMsg.LockScript.Length;
            coinbaseOutputMsg.Index = 0;

            if (newBlockReward < 0 || totalFee < 0 || coinbaseOutputMsg.Amount < 0)
            {
                LogHelper.Warn($"newBlockReward:{newBlockReward}");
                LogHelper.Warn($"totalFee:{totalFee}");
                LogHelper.Warn($"coinbaseOutputMsg.Amount:{coinbaseOutputMsg.Amount}");
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.COINBASE_OUTPUT_AMOUNT_ERROR);
            }

            coinbaseTxMsg.Hash = coinbaseTxMsg.GetHash();

            newBlockMsg.Transactions.Insert(0, coinbaseTxMsg);


            foreach (var tx in transactionMsgs)
            {
                newBlockMsg.Transactions.Add(tx);
            }                                                                                           

            headerMsg.PayloadHash = newBlockMsg.GetPayloadHash();

            LogHelper.Warn($"{headerMsg.Height}::{headerMsg.PayloadHash}");
            LogHelper.Warn($"PayloadHash::{headerMsg.PayloadHash}");
            LogHelper.Warn($"BlockSignature::{headerMsg.BlockSignature}");
            LogHelper.Warn($"miner::{minerName}");

            headerMsg.BlockSignature = Base16.Encode(HashHelper.EmptyHash());
            headerMsg.BlockSigSize = headerMsg.BlockSignature.Length;
            headerMsg.TotalTransaction = newBlockMsg.Transactions.Count;
            return newBlockMsg;
        }

        /// <summary>
        /// 估算交易费率
        /// </summary>
        /// <returns></returns>
        public long EstimateSmartFee()
        {
            //对象初始化
            var txDac = TransactionDac.Default;
            var transactionMsgs = new List<TransactionMsg>();
            var txPool = TransactionPool.Instance;
            long totalSize = 0;
            long totalFee = 0;
            //设置最大上限
            long maxSize = Consensus.BlockSetting.MAX_BLOCK_SIZE - (1 * 1024);
            //交易池中的项目按照费率从高到低排列
            List<TransactionPoolItem> poolItemList = TransactionPoolDac.Default.GetAllTx().OrderByDescending(x => x.FeeRate).ToList();
            var index = 0;

            while (totalSize < maxSize && index < poolItemList.Count)
            {
                //获取totalFee和totalSize
                TransactionMsg tx = poolItemList[index].Transaction;
                //判断交易Hash是否在交易Msg中
                if (tx != null && transactionMsgs.Where(t => t.Hash == tx.Hash).Count() == 0)
                {
                    totalFee += Convert.ToInt64(poolItemList[index].FeeRate * tx.Serialize().LongLength / 1024.0);
                    if (txDac.GetTransaction(tx.Hash) == null)
                    {
                        transactionMsgs.Add(tx);
                        totalSize += tx.Size;
                    }
                    else
                    {
                        txPool.RemoveTransaction(tx.Hash);
                    }
                }
                /*
                else
                {
                    break;
                }
                */
                index++;
            }
            //获取费率
            if (poolItemList.Count == 0)
            {
                return 1024;
            }
            long feeRate = Convert.ToInt64(Math.Ceiling((totalFee / (totalSize / 1024.0)) / poolItemList.Count));
            if (feeRate < 1024)
            {
                feeRate = 1024;
            }
            return feeRate;
        }

        public bool SaveBlockIntoDB(BlockMsg msg)
        {
            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                var result = VerifyBlock(msg);
                stopwatch.Stop();
                //LogHelper.Debug($"VerifyBlock ::{stopwatch.ElapsedMilliseconds}");

                if (!result)
                    return false;

                stopwatch.Reset();
                stopwatch.Restart();
                BlockDac.Default.Save(msg);
                stopwatch.Stop();
                //LogHelper.Debug($"BlockDac.Default.Save ::{stopwatch.ElapsedMilliseconds}");

                GlobalParameters.LocalHeight = msg.Header.Height;
                GlobalParameters.LatestBlockTime = msg.Header.Timestamp;
                BlockDac.Default.SetLastBlockHeight(msg.Header.Height);
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
                throw ex;
            }
        }

        public bool VerifyBlockBasic(BlockMsg newBlock)
        {
            if (newBlock.Header.Hash != newBlock.Header.GetHash())
            {
                throw new CommonException(ErrorCode.Engine.Block.Verify.BLOCK_HASH_ERROR);
            }

            bool hasPreviousBlock = false;
            long previousBlockTimestamp = -1;
            long previousBlockBits = -1;
            long previousBlockHeight = newBlock.Header.Height - 1;

            var cacheBlockKey = $"{newBlock.Header.Hash}_{newBlock.Header.Height - 1}";

            var previousBlockMsg = BlockDac.Default.SelectByHash(newBlock.Header.PreviousBlockHash);
            
            if (previousBlockMsg != null)
            {
                hasPreviousBlock = true;
                previousBlockTimestamp = previousBlockMsg.Header.Timestamp;
                previousBlockBits = previousBlockMsg.Header.Bits;
            }
            else
            {
                var previousBlock = BlockDac.Default.SelectByHash(newBlock.Header.PreviousBlockHash);
                hasPreviousBlock = previousBlock != null;
                if (hasPreviousBlock)
                {
                    previousBlockTimestamp = previousBlock.Header.Timestamp;
                    previousBlockBits = previousBlock.Header.Bits;
                }
            }

            if (newBlock.Header.Height > 0 && !hasPreviousBlock)
            {
                throw new CommonException(ErrorCode.Engine.Block.Verify.PREV_BLOCK_NOT_EXISTED);
            }

            if ((newBlock.Header.Timestamp - Time.EpochTime) > 2 * 60 * 60 * 1000 ||
                (hasPreviousBlock && newBlock.Header.Timestamp <= previousBlockTimestamp))
            {
                throw new CommonException(ErrorCode.Engine.Block.Verify.BLOCK_TIME_IS_ERROR);
            }

            if (newBlock.Serialize().Length > Consensus.BlockSetting.MAX_BLOCK_SIZE)
            {
                throw new CommonException(ErrorCode.Engine.Block.Verify.BLOCK_SIZE_LARGE_THAN_LIMIT);
            }

            BlockMsg prevStepBlock = null;
            if (newBlock.Header.Height >= POC.DIFFIUCLTY_ADJUST_STEP)
            {
                var prevStepHeight = 0L;
                if (!GlobalParameters.IsTestnet &&
                    newBlock.Header.Height <= POC.DIFFICULTY_CALCULATE_LOGIC_ADJUST_HEIGHT)
                {
                    prevStepHeight = newBlock.Header.Height - POC.DIFFIUCLTY_ADJUST_STEP - 1;
                }
                else
                {
                    prevStepHeight = newBlock.Header.Height - POC.DIFFIUCLTY_ADJUST_STEP;
                }
                prevStepBlock = BlockDac.Default.SelectByHeight(prevStepHeight);
            }

            //区块必须包含交易，否则错误
            if (newBlock.Transactions == null || newBlock.Transactions.Count == 0)
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.NOT_FOUND_COINBASE);
            }

            //第一个一定是coinbase,奖励+手续费
            var coinbase = newBlock.Transactions[0];
            //校验打包区块的矿池是否经过验证
            var minerInfo = Encoding.UTF8.GetString(Base16.Decode(coinbase.Inputs[0].UnlockScript)).Split("`")[0];
            var pool = MiningPoolComponent.CurrentMiningPools.FirstOrDefault(x => x.Name == minerInfo);
            if (pool == null)
            {
                throw new CommonException(ErrorCode.Engine.Block.Verify.MINING_POOL_NOT_EXISTED);
            }

            //校验区块的签名信息
            if (!POC.VerifyBlockSignature(newBlock.Header.PayloadHash, newBlock.Header.BlockSignature, pool.PublicKey))
            {
                LogHelper.Warn($"PayloadHash::{newBlock.Header.PayloadHash}");
                LogHelper.Warn($"BlockSignature::{newBlock.Header.BlockSignature}");
                LogHelper.Warn($"PublicKey::{pool.PublicKey}");
                throw new CommonException(ErrorCode.Engine.Block.Verify.BLOCK_SIGNATURE_IS_ERROR);
            }

            if (POC.CalculateBaseTarget(newBlock.Header.Height, previousBlockBits, previousBlockTimestamp,
                    prevStepBlock) != newBlock.Header.Bits)
            {
                throw new CommonException(ErrorCode.Engine.Block.Verify.BITS_IS_WRONG);
            }
            
            var targetResult = POC.CalculateTargetResult(newBlock);
            
            if (newBlock.Header.Height == 17687 || POC.Verify(newBlock.Header.Bits, targetResult))
            {
                return true;
            }
            else
            {
                throw new CommonException(ErrorCode.Engine.Block.Verify.POC_VERIFY_FAIL);
            }
        }

        readonly long[] Heights = new long[] { 19288, 19306, 19314, 19329 };

        public bool VerifyBlock(BlockMsg newBlock)
        {
            //校验区块的基本信息
            var result = VerifyBlockBasic(newBlock);
            if (!result)
                return false;

            var txComponent = new TransactionComponent();
            var blockComponent = new BlockComponent();

            //校验交易信息
            var totalFee = 0L;
            VerifyTransactionModel model = new VerifyTransactionModel();
            model.block = newBlock;
            model.localHeight = blockComponent.GetLatestHeight();
            foreach (var item in newBlock.Transactions)
            {
                long fee;
                model.transaction = item;

                if (txComponent.VerifyTransactionMsg(model, out fee))
                {
                    totalFee += fee;
                }
                else
                {
                    return false;
                }
            }

            if (Heights.Contains(newBlock.Header.Height))
                return true;

            var newBlockReward = POC.GetNewBlockReward(newBlock.Header.Height);
            var coinbaseAmount = newBlock.Transactions[0].Outputs[0].Amount;
            if (coinbaseAmount < 0 || coinbaseAmount != (totalFee + newBlockReward))
            {
                throw new CommonException(ErrorCode.Engine.Transaction.Verify.COINBASE_OUTPUT_AMOUNT_ERROR);
            }
            return true;
        }

        public long GetLatestHeight()
        {
            if (GlobalParameters.LocalHeight < 0)
            {
                var dac = BlockDac.Default;
                var block = dac.SelectLast();

                if (block != null)
                {
                    GlobalParameters.LocalHeight = block.Header.Height;
                    if (GlobalParameters.LatestBlockTime == 0)
                    { 
                        GlobalParameters.LatestBlockTime = block.Header.Timestamp;
                    }
                }
            }

            return GlobalParameters.LocalHeight;
        }

        public long GetLatestConfirmedHeight()
        {
            if (GlobalParameters.LocalConfirmedHeight < 0)
            {
                var dac = BlockDac.Default;
                var block = dac.SelectLastConfirmed();

                if (block != null)
                {
                    GlobalParameters.LocalConfirmedHeight = block.Header.Height;
                }
            }

            return GlobalParameters.LocalConfirmedHeight;
        }

        public List<BlockHeaderMsg> GetBlockHeaderMsgByHeights(List<long> heights)
        {
            var blocks= BlockDac.Default.SelectByHeights(heights);
            return blocks.Select(x => x.Header).ToList();
        }

        public List<BlockMsg> GetBlockMsgByHeights(List<long> heights)
        {
            var blocks = BlockDac.Default.SelectByHeights(heights);
            return blocks.ToList();
        }

        public BlockMsg GetBlockMsgByHeight(long height)
        {
            return BlockDac.Default.SelectByHeight(height);
        }

        public BlockMsg GetBlockMsgByHash(string hash)
        {
            return BlockDac.Default.SelectByHash(hash);
        }

        /// <summary>
        /// DaemonToolController专用
        /// </summary>
        /// <returns></returns>
        public long GetLatestBlockTimestamp()
        {
            return BlockDac.Default.SelectLast().Header.Timestamp;
        }

        public List<Block> GetBlockEntityByHash(IEnumerable<string> hashes)
        {
            var blockMsgs = BlockDac.Default.SelectByHashes(hashes);
            List<Block> result = new List<Block>();
            foreach (var msg in blockMsgs)
            {
                var entity = new Block
                {
                    Hash = msg.Header.Hash,
                    Version = msg.Header.Version,
                    Height = msg.Header.Height,
                    PreviousBlockHash = msg.Header.PreviousBlockHash,
                    Bits = msg.Header.Bits,
                    Nonce = msg.Header.Nonce,
                    Timestamp = msg.Header.Timestamp,
                    TotalAmount = msg.Transactions.SelectMany(x => x.Outputs).Sum(x => x.Amount),
                    GeneratorId = msg.Header.GeneratorId,
                    BlockSignature = msg.Header.BlockSignature,
                    IsDiscarded = false,
                    IsVerified = GlobalParameters.LocalHeight - msg.Header.Height >= 6
                };
                
                var totalfee = msg.Transactions[0].Outputs[0].Amount - POC.GetNewBlockReward(msg.Header.Height);
                if (totalfee > 0)
                    entity.TotalFee = totalfee;

                var nextBlockMsg = blockMsgs.FirstOrDefault(x => x.Header.Height == entity.Height + 1);
                if (nextBlockMsg == null)
                    nextBlockMsg = BlockDac.Default.SelectByHeight(msg.Header.Height + 1);

                if (entity != null)
                    entity.NextBlockHash = nextBlockMsg.Header.Hash;

                result.Add(entity);
            }
            return result;
        }

        public BlockHeaderMsg GetBlockHeaderMsgByHash(string hash)
        {
            var block = GetBlockMsgByHash(hash);
            if (block == null)
                return null;
            return block.Header;
        }

        public bool CheckBlockExists(string hash)
        {
            return BlockDac.Default.BlockHashExist(hash);
        }

        public bool CheckConfirmedBlockExists(long height)
        {
            var result = BlockDac.Default.SelectByHeight(height);
            if (result == null)
                return false;
            return BlockDac.Default.SelectLast().Header.Height - result.Header.Height >= 6;
        }
        
        public string GetMiningWorkResult(BlockMsg block)
        {
            var listBytes = new List<Byte>();
            listBytes.AddRange(Base16.Decode(block.Header.PayloadHash));
            listBytes.AddRange(BitConverter.GetBytes(block.Header.Height));
            var genHash = Sha3Helper.Hash(listBytes.ToArray());
          
            var blockData = new List<byte>();

            foreach (var tx in block.Transactions)
            {
                blockData.AddRange(tx.Serialize());
            }

            var nonceBytes = BitConverter.GetBytes(block.Header.Nonce);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(nonceBytes);
            }

            blockData.AddRange(nonceBytes);
            var result = Base16.Encode(
                HashHelper.Hash(
                    blockData.ToArray()
                ));

            return result;
        }
        
        public ListSinceBlock ListSinceBlock(string blockHash, long confirmations)
        {
            ListSinceBlock result = new ListSinceBlock();

            var lastHeight = GlobalParameters.LocalHeight;
            var endHeight = lastHeight - confirmations;
            result.LastBlock = BlockDac.Default.SelectByHeight(endHeight)?.Header.Hash;

            var startBlock = BlockDac.Default.SelectByHash(blockHash);
            if (startBlock == null)
            {
                return result;
            }

            var utxos = new List<UtxoSet>();
            var sinceBlockTxHashes = UtxoSetDac.Default.GetUtxoSetKeys(startBlock.Header.Height, lastHeight);
            utxos.AddRange(UtxoSetDac.Default.Get(sinceBlockTxHashes));
            
            AccountComponent component = new AccountComponent();
            List<SinceBlock> sinceBlocks = new List<SinceBlock>();
            foreach (var item in utxos)
            {
                sinceBlocks.Add(new SinceBlock
                {
                    Account = item.Account,
                    Address = item.Account,
                    amount = item.Amount,
                    BlockHash = item.BlockHash,
                    BlockTime = item.BlockTime,
                    Confirmations = lastHeight - item.BlockHeight,
                    IsSpent = item.IsSpent(),
                    LockTime = item.Locktime,
                    TxId = item.TransactionHash,
                    Category = !item.IsCoinbase ? "receive" : (item.IsConfirmed(lastHeight) ? "generate" : "immature"),
                    Vout = item.Index,
                    Label = component.GetAccountById(item.Account)?.Tag
                });
            }
            result.Transactions = sinceBlocks.ToArray();
            return result;
        }

        public ListSinceBlock ListPageSinceBlock(string blockHash, long confirmations, int currentPage, int pageSize)
        {
            ListSinceBlock result = new ListSinceBlock();

            var lastHeight = GlobalParameters.LocalHeight;
            var endHeight = lastHeight - confirmations;
            result.LastBlock = BlockDac.Default.SelectByHeight(endHeight)?.Header.Hash;

            var startBlock = BlockDac.Default.SelectByHash(blockHash);
            if (startBlock == null)
            {
                return result;
            }
            var utxos = new List<UtxoSet>();
            var sinceBlockTxHashes = UtxoSetDac.Default.GetUtxoSetKeys(startBlock.Header.Height, lastHeight);
            var skipCount = (currentPage - 1) * pageSize;
            var takeCount = pageSize;

            if (takeCount > 0)
            {
                sinceBlockTxHashes = sinceBlockTxHashes.Skip(skipCount).Take(takeCount).ToList();
                utxos.AddRange(UtxoSetDac.Default.Get(sinceBlockTxHashes));
            }

            AccountComponent component = new AccountComponent();
            List<SinceBlock> sinceBlocks = new List<SinceBlock>();
            foreach (var item in utxos)
            {
                sinceBlocks.Add(new SinceBlock
                {
                    Account = item.Account,
                    Address = item.Account,
                    amount = item.Amount,
                    BlockHash = item.BlockHash,
                    BlockTime = item.BlockTime,
                    Confirmations = lastHeight - item.BlockHeight,
                    IsSpent = item.IsSpent(),
                    LockTime = item.Locktime,
                    Category = !item.IsCoinbase ? "receive" : (item.IsConfirmed(lastHeight) ? "generate" : "immature"),
                    TxId = item.TransactionHash,
                    Vout = item.Index,
                    Label = component.GetAccountById(item.Account)?.Tag
                });
            }
            result.Transactions = sinceBlocks.ToArray();
            return result;
        }

        /// <summary>
        /// 获取区块总的奖励
        /// </summary>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        public long GetBlockReward(string blockHash)
        {
            //总的奖励分为两部分，一部分挖矿所得，一部分区块中交易手续费
            var block = BlockDac.Default.SelectByHash(blockHash);
            if (block == null)
                return 0;

            var totalOutput = block.Transactions.SelectMany(x => x.Outputs).Sum(x => x.Amount);
            var totalInputHashIndexs = block.Transactions.Where(x=>x.Hash!= "0000000000000000000000000000000000000000000000000000000000000000").SelectMany(x => x.Inputs).Select(x => x.OutputTransactionHash +"_"+ x.OutputIndex);
            if (totalInputHashIndexs.Count() == 0)
                return totalOutput;

            var totalInput = UtxoSetDac.Default.Get(totalInputHashIndexs).Sum(x=>x.Amount);

            return totalOutput - totalInput;
        }

        public BlockInfo GetBlockInfo(long height)
        {
            var block = BlockDac.Default.SelectByHeight(height);
            if (block == null)
                return null;

            var totalAmount = block.Transactions.SelectMany(x => x.Outputs).Sum(x => x.Amount);

            BlockInfo result = new BlockInfo();
            result.BlockHash = block.Header.Hash;
            result.Height = block.Header.Height;
            result.TradeCount = block.Header.TotalTransaction;
            result.TotalAmount = totalAmount;
            result.TotalSize = block.Transactions.Sum(x => x.Size);
            return result;
        }

        public BlockDetail GetBlockDetail(string hash)
        {
            var block = BlockDac.Default.SelectByHash(hash);
            if (block == null)
                return null;

            BlockDetail detail = new BlockDetail();
            detail.TradeCount = block.Header.TotalTransaction;
            detail.TotalOutput = block.Transactions.SelectMany(x => x.Outputs).Where(x => x.Amount > 0).Sum(x => x.Amount);
            var reward = POC.GetNewBlockReward(block.Header.Height);

            detail.Height = block.Header.Height;
            detail.Timestamp = block.Header.Timestamp;
            detail.Difficulty = POC.CalculateDifficulty(block.Header.Bits);
            detail.Bits = block.Header.Bits;
            detail.Version = block.Header.Version;
            detail.Nonce = block.Header.Nonce;
            detail.BlockReward = reward;
            detail.Hash = block.Header.Hash;
            detail.PreviousBlockHash = block.Header.PreviousBlockHash;
            var nextHash = BlockDac.Default.GetBlockHashByHeight(detail.Height + 1);
            detail.NextBlockHash = nextHash;


            detail.TranList = new List<TransOM>();

            block.Transactions.ForEach(x => {
                detail.TranList.Add(x.ConvertToDetail());
            });

            var tx = detail.TranList.Skip(0);
            var totalFee = tx.SelectMany(x => x.OutputList).Sum(x => x.Amount) - tx.SelectMany(x => x.InputList).Sum(x => x.Amount);
            detail.TransactionFees = totalFee;
            return detail;
        }
    }
}
