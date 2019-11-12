

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace OmniCoin.Consensus.Api
{
    public class ApiResponse
    {
        public ApiResponse()
        {
        }

        /// <param name="id">Request id</param>
        /// <param name="error">Request error</param>
        public ApiResponse(ApiError error)
        {
            this.Error = error;
        }

        /// <param name="id">Request id</param>
        /// <param name="result">Response result object</param>
        public ApiResponse(JToken result)
        {
            this.Result = result;
        }

        /// <summary>
        /// Reponse result object (Required)
        /// </summary>
        public JToken Result { get; set; }

        /// <summary>
        /// Error from processing Api request (Required)
        /// </summary>
        public ApiError Error { get; set; }

        public bool HasError => this.Error != null;

        public void ThrowErrorIfExists()
        {
            if (this.HasError)
            {
                throw this.Error.CreateException();
            }
        }
    }

    public static class ApiResponseExtensions
    {
        /// <summary>
        /// Parses and returns the result of the Api response as the type specified. 
        /// Otherwise throws a parsing exception
        /// </summary>
        /// <typeparam name="T">Type of object to parse the response as</typeparam>
        /// <param name="response">Api response object</param>
        /// <param name="returnDefaultIfNull">Returns the type's default value if the result is null. Otherwise throws parsing exception</param>
        /// <returns>Result of response as type specified</returns>
        public static T GetResult<T>(this ApiResponse response, bool returnDefaultIfNull = true, JsonSerializerSettings settings = null)
        {
            response.ThrowErrorIfExists();
            if (response.Result == null)
            {
                if (!returnDefaultIfNull && default(T) != null)
                {
                    throw new ApiClientParseException($"Unable to convert the result (null) to type '{typeof(T)}'");
                }
                return default(T);
            }
            try
            {
                if (settings == null)
                {
                    return response.Result.ToObject<T>();
                }
                else
                {
                    JsonSerializer jsonSerializer = JsonSerializer.Create(settings);
                    return response.Result.ToObject<T>(jsonSerializer);
                }
            }
            catch (Exception ex)
            {
                throw new ApiClientParseException($"Unable to convert the result to type '{typeof(T)}'", ex);
            }
        }
    }
}
