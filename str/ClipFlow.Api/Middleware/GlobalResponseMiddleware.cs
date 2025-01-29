using System.Text.Json;
using ClipFlow.Api.Models;
using ClipFlow.Models;

namespace ClipFlow.Api.Middleware
{
    public class GlobalResponseMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalResponseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;

            try
            {
                // 特殊处理文件流响应
                if (context.Request.Path.StartsWithSegments("/api/clipboard/file"))
                {
                    await _next(context);
                    return;
                }

                using var memoryStream = new MemoryStream();
                context.Response.Body = memoryStream;

                await _next(context);

                memoryStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();

                var statusCode = context.Response.StatusCode;
                object? responseObject;

                if (string.IsNullOrEmpty(responseBody))
                {
                    responseObject = new ApiResponse<object>
                    {
                        Code = statusCode,
                        Message = statusCode == 200 ? "Success" : "Error",
                        Data = null
                    };
                }
                else
                {
                    // 如果响应已经是 ApiResponse 格式，则不需要再包装
                    if (responseBody.Contains("\"code\":") && responseBody.Contains("\"message\":") && responseBody.Contains("\"data\":"))
                    {
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        await memoryStream.CopyToAsync(originalBodyStream);
                        return;
                    }

                    responseObject = new ApiResponse<object>
                    {
                        Code = statusCode,
                        Message = statusCode == 200 ? "Success" : "Error",
                        Data = JsonSerializer.Deserialize<object>(responseBody)
                    };
                }

                var jsonResponse = JsonSerializer.Serialize(responseObject);
                using var writer = new StreamWriter(originalBodyStream);
                context.Response.Body = originalBodyStream;
                context.Response.ContentType = "application/json";
                await writer.WriteAsync(jsonResponse);
                await writer.FlushAsync();
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
    }

    public static class GlobalResponseMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalResponse(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalResponseMiddleware>();
        }
    }
} 