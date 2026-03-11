using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace Shared;

public static class WebExtensions
{
    extension(IHttpContextAccessor contextAccessor)
    {
        public string? ExtractRouteValue(string key)
        {
            var context = contextAccessor.HttpContext; 
            if (context is null)
            {
                return null;
            }
            
            return context.GetRouteValue(key) as string;
        }
        
        public string? ExtractHeaderValue(string key)
        {
            var context = contextAccessor.HttpContext;
            if (context is null)
            {
                return null;
            }

            return context.Request.Headers.TryGetValue(key, out var header)
                ? header.Single()
                : null;
        }
    }
    
    extension(ClaimsPrincipal claimsPrincipal)
    {
        public Guid ExtractUserId()
        {
            return Guid.TryParse(claimsPrincipal.FindFirstValue(JwtRegisteredClaimNames.Sub) 
                                 ?? claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) 
                ? id 
                : Guid.Empty;
        }
    }
}
