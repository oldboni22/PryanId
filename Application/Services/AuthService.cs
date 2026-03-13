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

    public Task<Result<TokenPair>> LoginAsync(LoginUserModel loginUserModel,  CancellationToken ct = default);
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
            .FirstOrDefault(t => t.Token == model.OldTokenLiteral);

        if (refreshToken is null)
        {
            return Result<TokenPair>.FromError(AuthErrors.InvalidRefreshToken);
        }
        
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
        
        var pair = await jwtProvider.Generate(user.Email!, user.Id);
        
        user.RevokeRefreshToken(model.OldTokenLiteral, currentTime);
        
        dbContext.RefreshTokens.Add(CreateRefreshToken(pair.RefreshToken, user));
        await dbContext.SaveChangesAsync(ct);
        
        return Result<TokenPair>.Success(pair);
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
    
    public async Task<Result<TokenPair>> LoginAsync(LoginUserModel loginUserModel, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(loginUserModel.Email);
        
        if (user is null || !await userManager.CheckPasswordAsync(user, loginUserModel.Password))
        {
            if (user is not null)
            {
                await userManager.AccessFailedAsync(user);
            }
            
            return Result<TokenPair>.FromError(DomainErrors.InvalidCredentials);
        }
        
        await userManager.ResetAccessFailedCountAsync(user);
        
        var pair = await jwtProvider.Generate(user.Email!, user.Id);

        dbContext.RefreshTokens.Add(CreateRefreshToken(pair.RefreshToken, user));
        await dbContext.SaveChangesAsync(ct);
        
        return Result<TokenPair>.Success(pair);
    }

    private RefreshToken CreateRefreshToken(string token, User user)
    {
        return new RefreshToken
        {
            Token = token,
            CreatedAt = timeProvider.UtcNow,
            ExpiresAt = RefreshTokenExpiry(),
            UserId = user.Id
        };
    }
    
    private DateTime RefreshTokenExpiry() => timeProvider.UtcNow.Add(TimeSpan.FromDays(options.Value.RefreshExpiryDays));
}
