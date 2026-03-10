using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Shared;

public static class ClaimsPrincipalExtensions
{
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
