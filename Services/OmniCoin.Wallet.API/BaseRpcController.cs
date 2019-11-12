


using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Router.Defaults;
using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniCoin.Wallet.API
{
    public class BaseRpcController : RpcController
    {
        public RpcMethodErrorResult Error(int errorCode, string message, Exception exception = null)
        {
            if(errorCode != ErrorCode.UNKNOWN_ERROR)
            {
                LogHelper.Error(message, exception);
            }
            else
            {
                LogHelper.Fatal(message, exception);
            }

            return base.Error(errorCode, message, new { Time = Time.EpochTime });
        }
    }
}
