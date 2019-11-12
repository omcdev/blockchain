


using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmniCoin.DTO;
using OmniCoin.Framework;
using OmniCoin.Business;
using OmniCoin.Messages;
using OmniCoin.Consensus;
using OmniCoin.Entities;
using OmniCoin.Entities.CacheModel;
using OmniCoin.DataAgent;
using OmniCoin.Data.Dacs;

namespace OmniCoin.Wallet.API
{
    public class MemPoolController : BaseRpcController
    {
        public IRpcMethodResult GetAllTxInMemPool()
        {
            try
            {
                var result = new TransactionComponent().GetAllHashesFromPool();
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

        public IRpcMethodResult GetPaymentInfoInMemPool(string txHash)
        {
            try
            {
                List<PaymentOM> result = new List<PaymentOM>();
                var paymentFilters = PaymentDac.Default.GetAllFilter();
                Func<string, bool> condition = x => txHash.Equals(x);
                paymentFilters = paymentFilters.Where(x => condition(x.txId)).OrderBy(x => x.time).ToList();
                var payments = PaymentDac.Default.GetPayments(paymentFilters.Select(x => x.ToString()));

                var height = GlobalParameters.LocalHeight;
                foreach (var item in payments)
                {
                    result.Add(new PaymentOM
                    {
                        account = item.account,
                        amount = item.amount,
                        address = item.address,
                        blockHash = item.blockHash,
                        blockIndex = item.blockIndex,
                        blockTime = item.blockTime,
                        category = item.category,
                        comment = item.comment,
                        confirmations = string.IsNullOrEmpty(item.blockHash) ? 0 : height - BlockDac.Default.SelectByHash(item.blockHash).Header.Height,
                        fee = item.fee,
                        size = item.size,
                        time = item.time,
                        totalInput = item.totalInput,
                        totalOutput = item.totalOutput,
                        txId = item.txId,
                        vout = item.vout
                    });
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

    }
}
