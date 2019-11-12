

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using Newtonsoft.Json.Linq;
using System;

namespace OmniCoin.Consensus.Api
{
    public class ApiError
    {
        private ApiError() { }

        public ApiError(ApiException exception, bool showServerExceptions)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }
            this.Code = (int)exception.ErrorCode;
            this.Message = ApiError.GetErrorMessage(exception, showServerExceptions);
            this.Data = exception.ApiData;
        }

        /// <param name="code">Api error code</param>
        /// <param name="message">Error message</param>
        /// <param name="data">Optional error data</param>
        public ApiError(ApiErrorCode code, string message, JToken data = null) : this((int)code, message, data)
        {
        }

        /// <param name="code">Api error code</param>
        /// <param name="message">Error message</param>
        /// <param name="data">Optional error data</param>
        public ApiError(int code, string message, JToken data = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException(nameof(message));
            }
            this.Code = code;
            this.Message = message;
            this.Data = data;
        }

        public int Code { get; private set; }

        public string Message { get; private set; }

        public JToken Data { get; private set; }

        public ApiException CreateException()
        {
            ApiException exception;
            switch ((ApiErrorCode)this.Code)
            {
                case ApiErrorCode.ParseError:
                    exception = new ApiParseException(this);
                    break;
                case ApiErrorCode.InvalidRequest:
                    exception = new ApiInvalidRequestException(this);
                    break;
                case ApiErrorCode.MethodNotFound:
                    exception = new ApiMethodNotFoundException(this);
                    break;
                case ApiErrorCode.InvalidParams:
                    exception = new ApiInvalidParametersException(this);
                    break;
                case ApiErrorCode.InternalError:
                    exception = new ApiInvalidParametersException(this);
                    break;
                default:
                    exception = new ApiCustomException(this);
                    break;
            }
            return exception;
        }

        private static string GetErrorMessage(Exception exception, bool showServerExceptions)
        {
            string message = exception.Message;
            if (showServerExceptions && exception.InnerException != null)
            {
                message += "\tInner Exception: " + ApiError.GetErrorMessage(exception.InnerException, showServerExceptions);
            }
            return message;
        }
    }
}
