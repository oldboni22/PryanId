using Application.Contracts.Db;
using Application.Services;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Application.BackgroundJobs;

public sealed class ExpiredRefreshTokenWipeJob(IUserDbContext dbContext, TimeProvider timeProvider)
{
    public async Task ExecuteAsync(Guid userId)
    {
        var currentTime = timeProvider.UtcNow;

        await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && (token.RevokedAt != null || token.ExpiresAt <= currentTime))
            .ExecuteDeleteAsync();
    }
}
