

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

namespace OmniCoin.Consensus.Api
{
    public static class ApiHelper
    {
        public static ApiResponse GetApi(string url, List<KeyValuePair<string, string>> parameters)
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> item in parameters)
            {
                if (!string.IsNullOrEmpty(sb.ToString()))
                {
                    sb.Append("&");
                }
                sb.Append(item.Key);
                sb.Append("=");
                sb.Append(item.Value);
            }
            string address = url + sb.ToString();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
            request.Method = "GET";
            request.ContentType = "application/json";
            string result = null;
            using (HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse())
            {
                using (Stream rspStream = webResponse.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(rspStream, Encoding.UTF8))
                    {
                        result = reader.ReadToEnd();
                    }
                }
            }
            ApiResponse response = new ApiResponse();
            response.Result = Newtonsoft.Json.Linq.JToken.Parse(result);
            return response;
        }

        public static string PostApi(string url, Dictionary<string, string> parameters)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            string strContent = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
            using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
            {
                dataStream.Write(strContent);
                dataStream.Close();
            }

            string result = null;
            using (HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse())
            {
                using (Stream rspStream = webResponse.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(rspStream, Encoding.UTF8))
                    {
                        result = reader.ReadToEnd();
                    }
                }
            }
            return result;
        }
    }
}
