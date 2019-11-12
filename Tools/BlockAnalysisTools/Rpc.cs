using Newtonsoft.Json;
using OmniCoin.DTO.Explorer;
using OmniCoin.Entities.Explorer;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlockAnalysisTools
{
    public class RpcParam
    {
        public string jsonrpc;
        public int id;
        public string method;
        public object[] @params;

        public static RpcParam CreateNew(string method, params object[] @params)
        {
            RpcParam rpcParam = new RpcParam();
            rpcParam.jsonrpc = "2.0";
            rpcParam.id = 1;
            rpcParam.method = method;
            rpcParam.@params = @params;
            return rpcParam;
        }

        public class RpcResult
        {
            public string id;
            public string jsonrpc;
            public object result;
            public ErrorMsg error;
        }

        public class RpcResult<T>
        {
            public string id;
            public string jsonrpc;
            public T result;
            public ErrorMsg error;
        }

        public class ErrorMsg
        {
            public int code;
            public string message;
            public string data;
        }

        public class RpcBLL
        {
            public static string UrlString ="";

            private static RpcResult<T> Post<T>(RpcParam param)
            {
                var client = new RestClient(UrlString);
                var requestPost = new RestRequest("", Method.POST);
                var json = JsonConvert.SerializeObject(param);
                requestPost.AddParameter("application/json", json, ParameterType.RequestBody);

                IRestResponse responsePost = client.Execute(requestPost);
                var contentPost = responsePost.Content;
                return JsonConvert.DeserializeObject<RpcResult<T>>(contentPost);
            }

            public static string GetBlockHashByHeight(long height)
            {
                RpcParam param = RpcParam.CreateNew("GetBlockHashByHeight", height);
                RpcResult<string> result = Post<string>(param);
                return result.result;
            }

            public static BlockDetail GetBlockInfo(string blockHash)
            {
                RpcParam param = RpcParam.CreateNew("GetBlockInfo", blockHash);
                RpcResult <BlockDetail> result = Post<BlockDetail>(param);
                return result.result;
            }

            public static long GetBlockCount()
            {
                RpcParam param = RpcParam.CreateNew("GetBlockCount", null);
                RpcResult<long> result = Post<long>(param);
                return result.result;
            }
        }
    }
}
