using Application.Contracts.Db;
using Application.Contracts.JWT;
using Application.Models.User;
using Domain;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.ResultPattern;

namespace Application.Services;

public interface IAuthService
{
    public Task<Result<TokenPair>> RefreshAsync(string oldTokenLiteral, CancellationToken ct = default);
    
    public Task<Result> InvalidateAllSessions(Guid userId, CancellationToken ct = default);

    public Task<Result<TokenPair>> LoginAsync(LoginUserModel loginUserModel);
}

public sealed class AuthService(
    IOptions<JwtOptions> options,
    IUserDbContext dbContext,
    UserManager<User> userManager,
    IJwtProvider jwtProvider,
    TimeProvider timeProvider) : IAuthService
{
    public async Task<Result<TokenPair>> RefreshAsync(string oldTokenLiteral, CancellationToken ct = default)
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

    public async Task<Result> InvalidateAllSessions(Guid userId, CancellationToken ct = default)
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
    
    public async Task<Result<TokenPair>> LoginAsync(LoginUserModel loginUserModel)
    {
        var result = Result.Success();
        
        var user = await userManager.FindByEmailAsync(loginUserModel.Email);
        
        if (user is null || !await userManager.CheckPasswordAsync(user, loginUserModel.Password))
        {
            result.AddError(DomainErrors.InvalidCredentials);
            return Result<TokenPair>.FromResult(result);
        }
        
        var isPasswordCorrect = await userManager.CheckPasswordAsync(user, loginUserModel.Password);

        if (!isPasswordCorrect)
        {
            await userManager.AccessFailedAsync(user);
            
            result.AddError(DomainErrors.InvalidCredentials);
            return Result<TokenPair>.FromResult(result);
        }
        
        await userManager.ResetAccessFailedCountAsync(user);
        
        var pair = await jwtProvider.Generate(user.Email!, user.Id);
        
        return Result<TokenPair>.FromResult(result, pair);
    }
}
