using Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Application.Auth;

public sealed record ClientRoleRequirement(UserClientRole MinimalRole) : IAuthorizationRequirement
{
    public const string Viewer = "Project.Viewer";
    
    public const string Editor = "Project.Editor";
    
    public const string Admin  = "Project.Admin";
    
    public const string Owner  = "Project.Owner";
}
