using System.Security.Claims;
using Application.Contracts.JWT;
using Duende.IdentityServer.Stores;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Application.JWT;

public class JwtProvider(
    JwtOptions options, ISigningCredentialStore credentialStore, TimeProvider timeProvider, JsonWebTokenHandler handler) : IJwtProvider
{
    public async Task<string> Generate(Guid userId, string email, IEnumerable<string> roles)
    {
        var credentials = await credentialStore.GetSigningCredentialsAsync();
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        
        return handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer =  options.Issuer,
            Audience = options.Audience,
            Expires = timeProvider.GetUtcNow().AddMinutes(options.ExpiryMinutes).UtcDateTime,
            
            SigningCredentials = credentials
        });
    }
}
