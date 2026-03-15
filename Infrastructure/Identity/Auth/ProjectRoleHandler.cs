using Application.Auth;
using Application.Contracts;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Infrastructure.Identity.Auth;

public sealed class ProjectRoleHandler(UserDbContext dbContext, IHttpContextAccessor contextAccessor)
    : AuthorizationHandler<ClientRoleRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ClientRoleRequirement requirement)
    {
        var clientId = contextAccessor.ExtractRouteValue(RouteParameters.ClientId);

        if (clientId is null)
        {
            return;
        }

        var userId = context.User.ExtractUserId();

        var hasAccess = await dbContext.UserClients
            .AnyAsync(x => x.UserId == userId && x.ClientId == clientId && x.Role >= requirement.MinimalRole);

        if (hasAccess)
        {
            context.Succeed(requirement);
        }
    }
}
