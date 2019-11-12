using MultiThreadingMiners.Api;
using OmniCoin.Tools;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiThreadingMiners
{
    class Program
    {
        static string urlBase = "http://localhost:12321";
        static Random ran = new Random(DateTime.Now.Millisecond);
        static void Main(string[] args)
        {
            Config config = ConfigurationTool.GetAppSettings<Config>("MultiThreadingMiners.conf.json", "MultiThreadingMinersSetting");
            if(config == null || string.IsNullOrEmpty(config.MySqlConnectString) || string.IsNullOrEmpty(config.PoolApiUrl))
            {
                throw new Exception("config read from MultiThreadingMiners.conf.json failed !!!");
            }

            urlBase = config.PoolApiUrl;
            MysqlHelper.CONNECTIONSTRING = config.MySqlConnectString;

            /* 1、调用GenerateNewAddress产生地址，然后读取外部文件，把对应的地址，SN， Account写进mysql数据库中
             * 2、每20-50个POS机一组，开一个单独的线程，循环调用Api
             * 3、开始挖矿，随机每隔30-60分钟调一次GetScoopNumber，SubmitMaxNonce（131072）
             * 4、随机每隔10-30分钟调一次GetPaidReward和GetUnPaidReward
             */

            //先从数据库中获取所有
            //string url = "http://poolapi-test.pos.io/api/Miners/GetScoopNumber?address=omnit4MBt7EpAFx8VcFdVSbAbDp1s4g4HfKA61";
            MysqlHelper mysql = new MysqlHelper();
            Dictionary<string, string> dicAddressSN = mysql.GetAllMiners();
            Dictionary<string, string> dicAddrMaxNonce = new Dictionary<string, string>();
            int index = 1;
            foreach(var i in dicAddressSN)
            {
                var maxNonce = ran.Next(39321, 131073); // 39321 约等于  131072 * 0.3 
                dicAddrMaxNonce.Add(i.Value, maxNonce.ToString());
                LogHelper.Info($"index : {index} ; Address : {i.Value} ; maxNonce : {maxNonce.ToString()}");
                index++;
            }
            while (true)
            {
                int indexOfArray = 1;

                foreach (var item in dicAddressSN)
                {
                    //string sn = $"POSTESTMINER{i.ToString("000")}";
                    //string address = dicAddressSN[sn];
                    string sn = item.Key;
                    string address = item.Value;
                    //调用接口GetScoopNumber, SubmitMaxNonce（131072)
                    ApiResponseData getScoopNumberResponse = GetApiResponse("GetScoopNumber", address);
                    LogHelper.Info($"get {address} scoop number result: {getScoopNumberResponse.Data}");

                    Dictionary<string, string> dicSubmitMaxNonce = new Dictionary<string, string>();
                    //string submitMaxNonceUrl = "http://poolapi-test.pos.io/api/Miners/SubmitMaxNonce";
                    string submitMaxNonceUrl = urlBase+"/api/Miners/SubmitMaxNonce";
                    dicSubmitMaxNonce.Add("Address", address);
                    //dicSubmitMaxNonce.Add("MaxNonce", "131072");
                    //var maxNonce = ran.Next(39321, 131073); // 39321 约等于  131072 * 0.3 
                    var tmpNonce = dicAddrMaxNonce[address];
                    dicSubmitMaxNonce.Add("MaxNonce", tmpNonce);
                    dicSubmitMaxNonce.Add("ScoopData", "");
                    dicSubmitMaxNonce.Add("ScoopNumber", getScoopNumberResponse.Data.ToString());
                    dicSubmitMaxNonce.Add("SN", sn);
                    string response = ApiHelper.PostApi(submitMaxNonceUrl, dicSubmitMaxNonce);
                    LogHelper.Info($"indexOfArray : {indexOfArray} ; get {address} Submit Max Nonce result: {response}");
                    LogHelper.Info($"get paid resward {address} result: {GetApiResponse("GetPaidReward", address).Data}");
                    LogHelper.Info($"get UnPaid resward {address} result: {GetApiResponse("GetUnPaidReward", address).Data}");
                    indexOfArray++;
                    System.Threading.Thread.Sleep(200);
                }
                LogHelper.Info("Sleep 5 Minutes ，Then Start Next Loop");
                System.Threading.Thread.Sleep(1000 * 60 * 5);
            }
        }

        /// <summary>
        /// 根据方法名获取api response
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        static ApiResponseData GetApiResponse(string methodName, string address)
        {
            string url = "";
            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
            switch (methodName)
            {
                case "GetScoopNumber":
                    url = $"{urlBase}/api/Miners/GetScoopNumber?address={address}";
                    break;
                case "GetPaidReward":
                    url = $"{urlBase}/api/Miners/GetPaidReward?address={address}";
                    break;
                case "GetUnPaidReward":
                    url = $"{urlBase}/api/Miners/GetUnPaidReward?address={address}";
                    break;
            }
            ApiResponse response = ApiHelper.GetApi(url, list);
            ApiResponseData data = response.GetResult<ApiResponseData>();
            return data;
        }

        //static string SubmitMaxNonce(string address, string sn)
        //{
        //    Dictionary<string, string> dicSubmitMaxNonce = new Dictionary<string, string>();
        //    string submitMaxNonceUrl = $"{urlBase}/api/Miners/SubmitMaxNonce";
            
        //    dicSubmitMaxNonce.Add("Address", address);
        //    dicSubmitMaxNonce.Add("MaxNonce", "131072");
        //    dicSubmitMaxNonce.Add("ScoopData", "");
        //    dicSubmitMaxNonce.Add("ScoopNumber", "4096");
        //    dicSubmitMaxNonce.Add("SN", sn);
        //    string response = ApiHelper.PostApi(submitMaxNonceUrl, dicSubmitMaxNonce);
        //    return response;
        //}
    }
}
