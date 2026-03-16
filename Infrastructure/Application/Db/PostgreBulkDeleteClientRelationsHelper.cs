using Application.Contracts.Db;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Application.Db;

public sealed class PostgreBulkDeleteClientRelationsHelper(UserDbContext dbContext) : IBulkDeleteClientRelationsHelper
{
    public async Task ExecuteAsync(string clientId, int bulk)
    {
        int deletedRows;
        
        do
        {
            deletedRows = await dbContext.Database.ExecuteSqlAsync($@"
                DELETE FROM ""UserClients""
                WHERE ctid IN (
                    SELECT ctid 
                    FROM ""UserClients""
                    WHERE ""ClientId"" = {clientId}
                    LIMIT {bulk}
                )");
        }
        while(deletedRows > 0);
    }
}
