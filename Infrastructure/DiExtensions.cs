using Application.Contracts.Db;
using Application.Contracts.JWT;
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
                .ConfigureIdentity()
                .AddJwtProvider(configuration);

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
    }
}
