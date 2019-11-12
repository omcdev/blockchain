using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Router.Abstractions;
using OmniCoin.Business;
using OmniCoin.Framework;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace OmniCoin.Wallet.API
{
    public class DaemonToolController : BaseRpcController
    {
        /// <summary>
        /// 获取最新的区块时间戳
        /// </summary>
        /// <returns></returns>
        public IRpcMethodResult GetLatestBlockTimestamp()
        {
            try
            {
                BlockComponent block = new BlockComponent();
                long timestamp = block.GetLatestBlockTimestamp();
                return Ok(timestamp);
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

        /// <summary>
        /// 根据txhash判断交易是否打包
        /// </summary>
        /// <param name="txHash"></param>
        /// <returns></returns>
        public IRpcMethodResult IsTxHashExists(string txHash)
        {
            try
            {
                TransactionComponent trans = new TransactionComponent();
                bool result = trans.CheckTxExisted(txHash, false);
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

        /// <summary>
        /// 判断交易是否合法
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public IRpcMethodResult IsTransactionValid(Dictionary<string, int> dic)
        {
            try
            {
                TransactionComponent trans = new TransactionComponent();
                bool result = trans.IsTransactionValid(dic);
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
