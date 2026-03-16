using Application.BackgroundJobs;
using Application.Contracts.BackgroundJob;
using Hangfire;

namespace Infrastructure.Application.BackgroundJob;

public sealed class HangfireBackgroundJobScheduler(IBackgroundJobClient client) : IBackgroundJobScheduler
{
    public void EnqueueExpiredRefreshTokenWipe(Guid userId)
    {
        client.Enqueue<ExpiredRefreshTokenWipeJob>(job => job.ExecuteAsync(userId));
    }

    public void EnqueueClientRelationsWipe(string clientId)
    {
        client.Enqueue<ClientRelationsWipeJob>(job => job.ExecuteAsync(clientId));
    }
}
