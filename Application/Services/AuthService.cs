using Application.Contracts.Db;
using Application.Contracts.JWT;
using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.ResultPattern;

namespace Application.Services;

public interface IAuthService
{
    public Task<Result<TokenPair>> RefreshAsync(string oldTokenLiteral, CancellationToken ct = default);
    
    public Task<Result> InvalidateAllSessions(Guid userId, CancellationToken ct = default);
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
            .First(t => t.Token == oldTokenLiteral);
        
        var currentTime = timeProvider.GetUtcNow().UtcDateTime;
        
        if (refreshToken!.RevokedAt is not null)
        {
            user.RevokeAllRefreshTokens(currentTime);
            await dbContext.SaveChangesAsync(ct);
            
            return Result<TokenPair>.FromError(AuthErrors.CompromisedRefreshToken);
        }
        
        if (refreshToken.HasExpired(currentTime))
        {
            return Result<TokenPair>.FromError(AuthErrors.ExpiredRefreshToken);
        }
        
        var tokens = await jwtProvider.Generate(user.Email!, user.Id);
        
        user.RevokeRefreshToken(oldTokenLiteral, currentTime);
        user.AddRefreshToken(tokens.RefreshToken, TimeSpan.FromDays(options.Value.RefreshExpiryDays), currentTime);
        
        await dbContext.SaveChangesAsync(ct);
        return Result<TokenPair>.Success(tokens);
    }

    public async Task<Result> InvalidateAllSessions(Guid userId, CancellationToken ct)
    {
        var user = await dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        
        if (user is null)
        {
            return Result<TokenPair>.FromError(DomainErrors.UserNotFound);
        }
        
        var currentTime = timeProvider.GetUtcNow().UtcDateTime;
        
        user.RevokeAllRefreshTokens(currentTime);
        await dbContext.SaveChangesAsync(ct);
        
        return Result.Success();
    }
}
