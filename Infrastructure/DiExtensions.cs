using Application.Contracts.BackgroundJob;
using Application.Contracts.Db;
using Application.Contracts.JWT;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.Application.BackgroundJob;
using Infrastructure.Application.Db;
using Infrastructure.Application.JWT;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Shared;
using Shared.Db;

namespace Infrastructure;

file static class SectionNames
{
    public const string IdentityStorage = $"{DbConnectionOptions.SectionName}:Identity";
    
    public const string HangfireStorage = $"{DbConnectionOptions.SectionName}:Hangfire";
}

public static class DiExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {
            services
                .AddSingleton(new JsonWebTokenHandler())
                .AddSingleton(TimeProvider.System)
                .AddUserContext(configuration)
                .AddTransient<IBulkDeleteClientRelationsHelper, PostgreBulkDeleteClientRelationsHelper>()
                .AddSingleton<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>()
                .ConfigureIdentity()
                .AddJwtProvider(configuration)
                .AddHangfireBgJobs(configuration);

            return services;
        }

        private IServiceCollection AddUserContext(IConfiguration configuration)
        {
            var connectionString = configuration.ExtractConnectionString(SectionNames.IdentityStorage);

            return 
                services
                    .AddDbContext<UserDbContext>(options => options.UseNpgsql(connectionString))
                    .AddScoped<IUserDbContext>(sp => sp.GetRequiredService<UserDbContext>());
        }

        private IServiceCollection AddJwtProvider(IConfiguration configuration)
        {
            services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
            
            return services
                .AddScoped<IJwtProvider, JwtProvider>();
        }

        private IServiceCollection AddHangfireBgJobs(IConfiguration configuration)
        {
            var connectionString = configuration.ExtractConnectionString(SectionNames.HangfireStorage);

            return services
                .AddHangfire(config =>
                    config.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)
                    ))
                .AddHangfireServer();
        }
    }
}
