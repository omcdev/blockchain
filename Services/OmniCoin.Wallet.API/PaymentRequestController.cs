


using EdjCase.JsonRpc.Router.Abstractions;
using OmniCoin.Business;
using OmniCoin.Framework;
using System;

namespace OmniCoin.Wallet.API
{
    public class PaymentRequestController : BaseRpcController
    {
        public IRpcMethodResult CreateNewPaymentRequest(string address, string tag, long amount, string comment)
        {
            try
            {
                var result = PaymentRequestComponent.Add(address, tag, comment, amount);
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

        public IRpcMethodResult DeletePaymentRequestsByIds(string[] ids)
        {
            try
            {
                PaymentRequestComponent.DeleteByIds(ids);
                return Ok();
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

        public IRpcMethodResult GetAllPaymentRequests()
        {
            try
            {
                var result = PaymentRequestComponent.GetAll();
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
