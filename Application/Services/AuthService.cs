using Application.Contracts.Db;
using Application.Contracts.JWT;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.ResultPattern;

namespace Application.Services;

public interface IAuthService
{
    Task<Result<TokenPair>> RefreshAsync(string oldTokenLiteral, CancellationToken ct = default);
}

public sealed class AuthService(
    IOptions<JwtOptions> options,
    IUserDbContext dbContext,
    IJwtProvider jwtProvider,
    TimeProvider timeProvider) : IAuthService
{
    public async Task<Result<TokenPair>> RefreshAsync(string oldTokenLiteral, CancellationToken ct)
    {
        var user = await dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == oldTokenLiteral), ct);

        if (user is null)
        {
            return Result<TokenPair>.FromError(AuthErrors.InvalidRefreshToken);
        }
        
        var refreshToken = user.RefreshTokens
            .FirstOrDefault(t => t.Token == oldTokenLiteral);

        if (refreshToken is null)
        {
            return Result<TokenPair>.FromError(AuthErrors.InvalidRefreshToken);
        }
        
        var currentTime = timeProvider.GetUtcNow().UtcDateTime;
        
        if (refreshToken.RevokedAt is not null)
        {
            user.RevokeAllRefreshTokens(currentTime);
            await dbContext.SaveChangesAsync(ct);
            
            return Result<TokenPair>.FromError(AuthErrors.CompromisedRefreshToken);
        }
        
        if (refreshToken.HasExpired(currentTime))
        {
            return Result<TokenPair>.FromError(AuthErrors.ExpiredRefreshToken);
        }
        
        var tokens = await jwtProvider.Generate(user);
        
        user.RevokeRefreshToken(oldTokenLiteral, currentTime);
        user.AddRefreshToken(tokens.RefreshToken, TimeSpan.FromDays(options.Value.RefreshExpiryDays), currentTime);
        
        await dbContext.SaveChangesAsync(ct);
        return Result<TokenPair>.Success(tokens);
    }
}
