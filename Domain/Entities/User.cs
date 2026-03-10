using System;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public sealed class User : IdentityUser<Guid>
{ 
    public IEnumerable<UserClient> UserClients { get; init; } = [];
    
    
    private readonly List<RefreshToken> _refreshTokens = [];
    
    public IReadOnlyCollection<RefreshToken> RefreshTokens =>  _refreshTokens;

    public void AddRefreshToken(string token, TimeSpan duration, DateTime currentTime)
    {
        _refreshTokens.Add(new RefreshToken 
        { 
            Id = Guid.NewGuid(),
            Token = token, 
            UserId = this.Id,
            CreatedAt = currentTime,
            ExpiresAt = currentTime.Add(duration)
        });
    }

    public void RevokeRefreshToken(string tokenLiteral, DateTime currentTime)
    {
        var token = _refreshTokens.SingleOrDefault(t => t.Token == tokenLiteral);
        
        token?.RevokedAt = currentTime;
    }
}
