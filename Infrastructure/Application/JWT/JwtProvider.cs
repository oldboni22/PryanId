using System.Security.Claims;
using System.Security.Cryptography;
using Application.Contracts.JWT;
using Domain.Entities;
using Duende.IdentityServer.Stores;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Application.JWT;

public sealed class JwtProvider(
    IOptions<JwtOptions> options, 
    ISigningCredentialStore credentialStore, 
    TimeProvider timeProvider, 
    JsonWebTokenHandler handler) : IJwtProvider
{
    public async Task<TokenPair> Generate(string email, Guid userId)
    {
        var accessToken = await GenerateAccessToken(userId, email);
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
            Issuer =  options.Value.Issuer,
            Audience = options.Value.Audience,
            
            Expires = timeProvider.GetUtcNow().AddMinutes(options.Value.AccessExpiryMinutes).UtcDateTime,
            IssuedAt = timeProvider.GetUtcNow().UtcDateTime,
            
            SigningCredentials = credentials
        });
    }
}
