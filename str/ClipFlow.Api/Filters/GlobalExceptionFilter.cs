using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ClipFlow.Api.Models;
using ClipFlow.Models;

namespace ClipFlow.Api.Filters
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "未处理的异常");

            var errorMessage = _env.IsDevelopment() 
                ? $"{context.Exception.Message}\n{context.Exception.StackTrace}"
                : "服务器内部错误";

            var response = ApiResponse<object>.Error(500, errorMessage);

            context.Result = new ObjectResult(response)
            {
                StatusCode = 500
            };
        }
    }
} 