namespace Application.Contracts.BackgroundJob;

public interface IBackgroundJobScheduler
{
    void EnqueueExpiredRefreshTokenWipe(Guid userId);

    void EnqueueClientRelationsWipe(string clientId);
}
