


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Framework
{
    public class CommonException : Exception
    {
        public CommonException(int errorCode)
        {
            this.ErrorCode = errorCode;
        }

        public CommonException(int errorCode, Exception innerException):base("", innerException)
        {
            this.ErrorCode = errorCode;            
        }

        public int ErrorCode { get; set; }

        public override string Message
        {
            get
            {
                return "Error Code: " + ErrorCode;
            }
        }
    }
}
