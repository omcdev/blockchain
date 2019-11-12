using OmniCoin.Consensus.Api;
using OmniCoin.Framework;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;

namespace OmniCoin.MiningPool.API
{
    public class BaseController : Controller
    {
        [ApiExplorerSettings(IgnoreApi = true)]
        public CommonResponse Error(int errorCode, string message, Exception exception = null)
        {
            if (errorCode != Entities.MiningPoolErrorCode.COMMON_ERROR)
            {
                LogHelper.Error(message);
            }
            else
            {
                LogHelper.Fatal(message, exception);
            }
            switch (errorCode)
            {
                case 10012:
                    errorCode = 1010008;
                    break;
                case 10020:
                    errorCode = 1010005;
                    break;
                case 10029:
                    errorCode = 1010006;
                    break;
                case 10180:
                    errorCode = 1010007;
                    break;
            }
            return new CommonResponse { Data = null, Code = errorCode, Extension = "", Message = message };
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public CommonResponse OK(object obj = null)
        {
            if (obj == null)
            {
                return new CommonResponse { Data = null, Code = 0, Extension = "", Message = "successful" };
            }
            return new CommonResponse { Data = Newtonsoft.Json.Linq.JToken.FromObject(obj), Code = 0, Extension = "", Message = "successful" };
        }
    }
}
