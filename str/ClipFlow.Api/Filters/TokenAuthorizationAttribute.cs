using Microsoft.AspNetCore.Mvc;

namespace ClipFlow.Api.Filters
{
    public class TokenAuthorizationAttribute : ServiceFilterAttribute
    {
        public TokenAuthorizationAttribute() : base(typeof(TokenAuthorizationFilter))
        {
        }
    }
} 