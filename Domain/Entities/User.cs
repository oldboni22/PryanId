using System;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public sealed class User : IdentityUser<Guid>
{ 
    public IEnumerable<UserClient> UserClients { get; init; } = [];
    
    
    private readonly List<RefreshToken> _refreshTokens = [];
    
    public IReadOnlyCollection<RefreshToken> RefreshTokens =>  _refreshTokens;

    public void RevokeRefreshToken(string tokenLiteral, DateTime currentTime)
    {
        var token = _refreshTokens.SingleOrDefault(t => t.Token == tokenLiteral);
        
        token?.RevokedAt = currentTime;
    }

    public void RevokeAllRefreshTokens(DateTime currentTime)
    {
        foreach (var token in _refreshTokens.Where(token => token.IsActive(currentTime)))
        {
            token.RevokedAt = currentTime;
        }
    }
}
