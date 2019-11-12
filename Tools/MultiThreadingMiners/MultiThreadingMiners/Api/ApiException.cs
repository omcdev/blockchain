

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using Newtonsoft.Json.Linq;
using System;

namespace MultiThreadingMiners.Api
{
    public abstract class ApiException : Exception
    {
        public int ErrorCode { get; }

        public JToken ApiData { get; }

        public string ErrorMessage { get; set; }

        protected ApiException(ApiError error) : this(error.Code, error.Message, error.Data)
        {
            //
        }

        public ApiException(int errorCode, string message, JToken data = null, Exception innerException = null)
        {
            ErrorCode = errorCode;
            ErrorMessage = message;
            ApiData = data;
        }
    }

    public class ApiInvalidRequestException : ApiException
    {
        internal ApiInvalidRequestException(ApiError error) : base(error)
        {
        }

        /// <param name="message">Error message</param>
        public ApiInvalidRequestException(string message) : base((int)ApiErrorCode.InvalidRequest, message)
        {
        }
    }

    /// <summary>
    /// Exception for requests that match no methods for invoking
    /// </summary>
    public class ApiMethodNotFoundException : ApiException
    {
        internal ApiMethodNotFoundException(ApiError error) : base(error)
        {
        }
        public ApiMethodNotFoundException() : base((int)ApiErrorCode.MethodNotFound, "No method found with the requested signature or multiple methods matched the request.")
        {
        }
    }

    /// <summary>
    /// Exception for requests that match a method but have invalid parameters
    /// </summary>
    public class ApiInvalidParametersException : ApiException
    {
        internal ApiInvalidParametersException(ApiError error) : base(error)
        {
        }
        public ApiInvalidParametersException(string message, Exception innerException = null) : base((int)ApiErrorCode.InvalidParams, message, null, innerException)
        {
        }
    }

    /// <summary>
    /// Exception for requests that have an unexpected or unknown exception thrown
    /// </summary>
    public class ApiUnknownException : ApiException
    {
        internal ApiUnknownException(ApiError error) : base(error)
        {
        }

        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception (optional)</param>
        public ApiUnknownException(string message, Exception innerException = null) : base((int)ApiErrorCode.InternalError, message, null, innerException)
        {
        }
    }

    /// <summary>
    /// Exception for requests that have parsing error
    /// </summary>
    public class ApiParseException : ApiException
    {
        internal ApiParseException(ApiError error) : base(error)
        {
        }

        /// <param name="message">Error message</param>
        public ApiParseException(string message) : base((int)ApiErrorCode.ParseError, message)
        {
        }
    }

    public abstract class ApiClientException : Exception
    {
        /// <param name="message">Error message</param>
        protected ApiClientException(string message) : base(message)
        {

        }

        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        protected ApiClientException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }

    public class ApiClientUnknownException : ApiClientException
    {
        /// <param name="message">Error message</param>
        public ApiClientUnknownException(string message) : base(message)
        {
        }

        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public ApiClientUnknownException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception for all parsing exceptions that were thro
    /// </summary>
    public class ApiClientParseException : ApiClientException
    {
        /// <param name="message">Error message</param>
        public ApiClientParseException(string message) : base(message)
        {
        }

        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public ApiClientParseException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Custom exception defined by the server
    /// </summary>
    public class ApiCustomException : ApiException
    {
        internal ApiCustomException(ApiError error) : base(error)
        {
        }

        public ApiCustomException(int code, string message, Exception innerException = null) : base(code, message, null, innerException)
        {
            
        }
    }

    public enum ApiErrorCode
    {
        ParseError = -32700,
        InvalidRequest = -32600,
        MethodNotFound = -32601,
        InvalidParams = -32602,
        InternalError = -32603
    }
}
