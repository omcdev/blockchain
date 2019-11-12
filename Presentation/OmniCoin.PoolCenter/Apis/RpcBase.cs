


using EdjCase.JsonRpc.Client;
using EdjCase.JsonRpc.Core;
using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OmniCoin.PoolCenter.Apis
{
    public abstract class RpcBase
    {
        RpcClient client;

        ManualResetEvent manualResetEvent = new ManualResetEvent(false);

        public RpcBase(string uri)
        {
            System.Net.Http.Headers.AuthenticationHeaderValue headerValue = null;
            this.client = new RpcClient(new Uri(uri), headerValue);
        }

        public T RunAsync<T>(Task<T> runTask)
        {
            T result = default(T);
            result = runTask.Result;
            return result;
        }

        public T SendRpcRequest<T>(string methodName, object[] parameters = null)
        {
            try
            {
                RpcRequest request = RpcRequest.WithParameterList(methodName, parameters, "Id1");
                RpcResponse result = RunAsync(client.SendRequestAsync(request));

                if (result.HasError)
                {
                    throw new Exception("Failed in send " + methodName + " RPC request, error code is " + result.Error.Code);
                }
                else
                {
                    return result.GetResult<T>();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed in send " + methodName + " rpc request, error code is UNKNOWN, please check and restart Node service \n");
            }
        }

        public void SendRpcRequest(string methodName, object[] parameters = null)
        {
            AutoResetEvent autoResetEvent = new AutoResetEvent(false);
            RpcRequest request = RpcRequest.WithParameterList(methodName, parameters, "Id1");
            RpcResponse result = RunAsync(client.SendRequestAsync(request));
            if (result.HasError)
            {
                var errorMsg = string.Format("error at Rpc Method \"{0}\" errorCode {1}, errorMsg {2}", methodName, result.Error.Code, result.Error.Message);
                throw new Exception(errorMsg);
            }
        }
    }
}
