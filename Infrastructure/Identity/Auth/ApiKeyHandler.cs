using Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Shared;

namespace Infrastructure.Identity.Auth;

public sealed class ApiKeyHandler(IHttpContextAccessor contextAccessor) : AuthorizationHandler<ApiKeyRequirement>
{
    private const string ApiKeyHeaderName = "X-API-KEY";
    
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ApiKeyRequirement requirement)
    {
        var key = contextAccessor.ExtractHeaderValue(ApiKeyHeaderName);

        if (key is not null && string.Equals(key, requirement.Key))
        {
            context.Succeed(requirement);    
        }
        
        return Task.CompletedTask;
    }
}
