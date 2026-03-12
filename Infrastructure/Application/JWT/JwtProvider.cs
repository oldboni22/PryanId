using System.Security.Claims;
using System.Security.Cryptography;
using Application.Contracts.JWT;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shared;

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

    private static string GenerateRefreshToken() => Base64UrlTextEncoder.Encode(RandomNumberGenerator.GetBytes(32));
    
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
            
            Expires = timeProvider.UtcNow.AddMinutes(options.Value.AccessExpiryMinutes),
            IssuedAt = timeProvider.UtcNow,
            
            SigningCredentials = credentials
        });
    }
}
