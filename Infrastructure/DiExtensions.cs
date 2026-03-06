using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using Shared.Db;

namespace Infrastructure;

file static class SectionNames
{
    public const string IdentityStorage = $"{DbConnectionOptions.SectionName}:Identity";
}

public static class DiExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {
            services
                .AddUserContext(configuration)
                .AddIdentity<User, IdentityRole<Guid>>()
                .AddEntityFrameworkStores<UserDbContext>();

            return services;
        }

        private IServiceCollection AddUserContext(IConfiguration configuration)
        {
            var connectionString = configuration.ExtractConnectionString(SectionNames.IdentityStorage);
            
            return services.AddDbContext<UserDbContext>(options => options.UseNpgsql(connectionString));
        }
    }
}
