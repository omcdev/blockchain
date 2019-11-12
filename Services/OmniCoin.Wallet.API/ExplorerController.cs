using EdjCase.JsonRpc.Router.Abstractions;
using OmniCoin.Business;
using OmniCoin.Consensus;
using OmniCoin.Data.Dacs;
using OmniCoin.Entities.Explorer;
using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniCoin.Wallet.API
{
    public class ExplorerController : BaseRpcController
    {
        public IRpcMethodResult GetLatestBlock()
        {
            try
            {
                List<BlockInfo> result = new List<BlockInfo>();
                var lastblock = BlockDac.Default.SelectLast();
                if (lastblock == null)
                    return Ok(result);

                BlockComponent component = new BlockComponent();

                for (int i = 0; i < 5; i++)
                {
                    var height = lastblock.Header.Height - i;
                    if (height < 0)
                        break;
                    var block = component.GetBlockInfo(height);
                    if (block != null)
                        result.Add(block);
                }
                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetBlockDataList(int skipCount, int takeCount)
        {
            try
            {
                List<BlockInfo> result = new List<BlockInfo>();
                var lastblock = BlockDac.Default.SelectLast();
                if (lastblock == null)
                    return Ok(result);

                BlockComponent component = new BlockComponent();
                long startHeight = lastblock.Header.Height - skipCount;
                long height = startHeight;
                for(long count = 1; count<= takeCount;count++)
                {                    
                    if (height < 0)
                        break;
                    var block = component.GetBlockInfo(height);
                    if (block != null)
                        result.Add(block);
                    height--;
                }

                //for (int i = skipCount; i < takeCount; i++)
                //{
                //    var height = lastblock.Header.Height - i;
                //    if (height < 0)
                //        break;
                //    var block = component.GetBlockInfo(height);
                //    if (block != null)
                //        result.Add(block);
                //}
                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetAddressInfo(string accountId)
        {
            try
            {
                AccountComponent component = new AccountComponent();
                var result = component.GetAccountInfo(accountId);
                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetBlockInfo(string blockHash)
        {
            try
            {
                BlockComponent component = new BlockComponent();
                var result = component.GetBlockDetail(blockHash);
                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetTransactionInfo(string transactionHash)
        {
            try
            {
                TransactionComponent component = new TransactionComponent();
                var result = component.GetTransactionDetail(transactionHash);
                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetBlockHashByHeight(long height)
        {
            try
            {
                var result = BlockDac.Default.GetBlockHashByHeight(height);
                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetHashTypeByHash(string hash)
        {
            try
            {
                if (BlockDac.Default.BlockHashExist(hash))
                    return Ok(0);
                if (TransactionDac.Default.HasTransaction(hash))
                    return Ok(1);
                else
                    return Ok(-1);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetNewBlockHeightBits()
        {
            try
            {
                var block = BlockDac.Default.SelectLast();
                if (block == null)
                    return Ok(null);

                return Ok(new { Height = block.Header.Height, Bits = block.Header.Bits });
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetNewBlockReward()
        {
            try
            {
                var block = BlockDac.Default.SelectLast();
                if (block == null)
                {
                    var reward = POC.GetNewBlockReward(0);
                    return Ok(reward);
                }
                else
                {
                    var reward = POC.GetNewBlockReward(block.Header.Height);
                    return Ok(reward);
                }
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetUtxoNoLock()
        {
            try
            {
                var amount = DataStatisticsDac.Default.GetAmountNoLock();
                return Ok(amount);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetUtxoAndLock()
        {
            try
            {
                var totalAmount = DataStatisticsDac.Default.GetTotalAmount();
                return Ok(totalAmount);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetAccountDataList(int skipCount,int takeCount)
        {
            try
            {
                var addressInfos = DataStatisticsDac.Default.GetAccountDataWithPage(skipCount, takeCount);
                return Ok(addressInfos);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }
    }
}