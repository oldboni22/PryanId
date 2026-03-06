using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
}
