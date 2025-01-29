using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ClipFlow.Api.Services;
using ClipFlow.Api.Models;
using ClipFlow.Models;

namespace ClipFlow.Api.Filters
{
    public class TokenAuthorizationFilter : IAuthorizationFilter
    {
        private readonly ClipboardWebSocketManager _webSocketManager;

        public TokenAuthorizationFilter(ClipboardWebSocketManager webSocketManager)
        {
            _webSocketManager = webSocketManager;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue("X-Auth-Token", out var token) || 
                !_webSocketManager.ValidateToken(token))
            {
                var response = ApiResponse<object>.Error(401, "未授权访问，请检查认证令牌");
                context.Result = new UnauthorizedObjectResult(response);
            }
        }
    }
} 