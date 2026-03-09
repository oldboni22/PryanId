using Application;
using Application.Contracts.JWT;
using Domain.Entities;
using Infrastructure.Application.JWT;
using Infrastructure.Identity;
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
                .AddSingleton(TimeProvider.System)
                .AddUserContext(configuration)
                .ConfigureIdentity()
                .AddJwtProvider(configuration);

            return services;
        }

        private IServiceCollection AddUserContext(IConfiguration configuration)
        {
            var connectionString = configuration.ExtractConnectionString(SectionNames.IdentityStorage);

            return services.AddDbContext<UserDbContext>(options => options.UseNpgsql(connectionString));
        }

        private IServiceCollection AddJwtProvider(IConfiguration configuration)
        {
            services.AddOptions<JwtOptions>()
                .Bind(configuration.GetSection(JwtOptions.SectionName))
                .ValidateOnStart();
            
            return services
                .AddScoped<IJwtProvider, JwtProvider>();
        }
    }
    
}
