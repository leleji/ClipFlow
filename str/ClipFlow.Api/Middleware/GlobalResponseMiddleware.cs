using System;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ClipFlow.Api.Models;
using ClipFlow.Api.Exceptions;
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
            // 如果是WebSocket请求，直接跳过
            if (context.WebSockets.IsWebSocketRequest)
            {
                await _next(context);
                return;
            }

            try
            {
                await _next(context);

                // 如果没有响应体，不需要处理
                if (!context.Response.HasStarted && context.Response.Body.Length == 0)
                {
                    var response = new ApiResponse<object>
                    {
                        Code = context.Response.StatusCode,
                        Message = "Success",
                        Data = null
                    };

                    await WriteResponseAsync(context, response);
                }
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            // 如果响应已经开始，无法修改
            if (context.Response.HasStarted)
            {
                throw ex;
            }

            var response = new ApiResponse<object>
            {
                Code = StatusCodes.Status500InternalServerError,
                Message = ex.Message,
                Data = null
            };

            if (ex is ApiException apiEx)
            {
                response.Code = apiEx.Code;
            }

            await WriteResponseAsync(context, response);
        }

        private async Task WriteResponseAsync<T>(HttpContext context, ApiResponse<T> response)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = response.Code;

            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                response,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
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