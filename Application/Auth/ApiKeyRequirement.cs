using Microsoft.AspNetCore.Authorization;

namespace Application.Auth;

public sealed record ApiKeyRequirement(string Key) : IAuthorizationRequirement
{
    public const string EmailRecovery = "EmailRecover";
}
