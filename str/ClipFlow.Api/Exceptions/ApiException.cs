using System;

namespace ClipFlow.Api.Exceptions
{
    public class ApiException : Exception
    {
        public int Code { get; }

        public ApiException(string message, int code = 500) : base(message)
        {
            Code = code;
        }

        public ApiException(string message, Exception innerException, int code = 500)
            : base(message, innerException)
        {
            Code = code;
        }
    }
} 