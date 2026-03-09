using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

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
    }
}
