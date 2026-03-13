using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Contracts.Db;
using Application.Contracts.JWT;
using Application.Models.User;
using Domain;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared;
using Shared.ResultPattern;

namespace Application.Services;

public interface IAuthService
{
    public Task<Result<TokenPair>> RefreshAsync(ReloginModel model, CancellationToken ct = default);
    
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
    public async Task<Result<TokenPair>> RefreshAsync(ReloginModel model, CancellationToken ct = default)
    {
        var user = await dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == model.OldTokenLiteral), ct);

        if (user is null)
        {
            return Result<TokenPair>.FromError(AuthErrors.InvalidRefreshToken);
        }
        
        var refreshToken = user.RefreshTokens
            .First(t => t.Token == model.OldTokenLiteral);
        
        var currentTime = timeProvider.UtcNow;
        
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
        
        user.RevokeRefreshToken(model.OldTokenLiteral, currentTime);
        user.AddRefreshToken(tokens.RefreshToken, RefreshTokenDuration, currentTime);
        
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
        
        var currentTime = timeProvider.UtcNow;
        
        user.RevokeAllRefreshTokens(currentTime);
        await dbContext.SaveChangesAsync(ct);
        
        return Result.Success();
    }
    
    public async Task<Result<TokenPair>> LoginAsync(LoginUserModel loginUserModel)
    {
        var user = await userManager.FindByEmailAsync(loginUserModel.Email);
        
        if (user is null || !await userManager.CheckPasswordAsync(user, loginUserModel.Password))
        {
            return Result<TokenPair>.FromError(DomainErrors.InvalidCredentials);
        }
        
        var isPasswordCorrect = await userManager.CheckPasswordAsync(user, loginUserModel.Password);

        if (!isPasswordCorrect)
        {
            await userManager.AccessFailedAsync(user);
            return Result<TokenPair>.FromError(DomainErrors.InvalidCredentials);
        }
        
        await userManager.ResetAccessFailedCountAsync(user);
        
        var pair = await jwtProvider.Generate(user.Email!, user.Id);
        
        user.AddRefreshToken(pair.RefreshToken, RefreshTokenDuration, timeProvider.UtcNow);
        await dbContext.SaveChangesAsync();
        
        return Result<TokenPair>.Success(pair);
    }

    private TimeSpan RefreshTokenDuration => TimeSpan.FromDays(options.Value.RefreshExpiryDays);
}
