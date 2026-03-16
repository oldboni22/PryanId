using Application.Contracts.Db;

namespace Application.BackgroundJobs;

public sealed class ClientRelationsWipeJob(IBulkDeleteClientRelationsHelper helper)
{
    private const int BulkDeleteIterationCount = 4000; 
    
    public async Task ExecuteAsync(string clientId)
    {
        await helper.ExecuteAsync(clientId, BulkDeleteIterationCount);
    }
}
