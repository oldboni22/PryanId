using System.Security.Claims;
using System.Security.Cryptography;
using Application.Contracts.JWT;
using Domain.Entities;
using Duende.IdentityServer.Stores;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Application.JWT;

public sealed class JwtProvider(
    JwtOptions options, ISigningCredentialStore credentialStore, TimeProvider timeProvider, JsonWebTokenHandler handler) : IJwtProvider
{
    public async Task<TokenPair> Generate(User user)
    {
        var accessToken = await GenerateAccessToken(user.Id, user.Email!);
        var refreshToken = GenerateRefreshToken();
        
        return new TokenPair(accessToken, refreshToken);
    }

    private static string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    
    private async Task<string> GenerateAccessToken(Guid userId, string email)
    {
        var credentials = await credentialStore.GetSigningCredentialsAsync();
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        
        return handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer =  options.Issuer,
            Audience = options.Audience,
            Expires = timeProvider.GetUtcNow().AddMinutes(options.ExpiryMinutes).UtcDateTime,
            IssuedAt = timeProvider.GetUtcNow().UtcDateTime,
            
            SigningCredentials = credentials
        });
    }
}
