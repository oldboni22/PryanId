using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Pagination;

namespace Shared.Db;

public static class DbExtensions
{
    extension(WebApplication app)
    {
        public void MigrateDatabases(params Type[] types)
        {
            using var scope = app.Services.CreateScope();

            foreach (var type in types)
            {
                var context = (DbContext)scope.ServiceProvider.GetRequiredService(type);
                
                context.Database.Migrate();
            }
        }
    }

    extension<T>(IQueryable<T> query)
    {
        public IQueryable<T> Page(PaginationParameters parameters)
        {
            return query
                .Skip(parameters.PageSize * (parameters.Page - 1))
                .Take(parameters.PageSize);
        }
    }
}
