using Application;
using Application.Auth;
using Application.Contracts.JWT;
using Application.Contracts.Options;
using Domain.Entities;
using Domain.Enums;
using Duende.IdentityServer;
using Infrastructure;
using Infrastructure.Identity.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Shared;
using Shared.Db;

namespace Api;

file static class SectionNames
{
    public const string ConfigurationalStorage = $"{DbConnectionOptions.SectionName}:Configurational";
    
    public const string OperationalStorage = $"{DbConnectionOptions.SectionName}:Operational";
}

internal static class HostingExtensions
{
    extension(WebApplicationBuilder builder)
    {
        private IIdentityServerBuilder ConfigureIdentityServer()
        {
            var jwtOptions = builder.Configuration.ExtractOptions<JwtOptions>(JwtOptions.SectionName);
            
            return builder.Services
                .AddIdentityServer(options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;

                    options.EmitStaticAudienceClaim = true;

                    options.IssuerUri = jwtOptions.Issuer;
                })
                .AddConfigurationStore(options =>
                {
                    var connectionString =
                        builder.Configuration.ExtractConnectionString(SectionNames.ConfigurationalStorage);
                    
                    options.ConfigureDbContext = b => b
                        .UseNpgsql(connectionString, dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName));
                })
                .AddConfigurationStoreCache()
                .AddOperationalStore(options =>
                {
                    var connectionString =
                        builder.Configuration.ExtractConnectionString(SectionNames.OperationalStorage);

                    options.ConfigureDbContext = b => b
                        .UseNpgsql(connectionString, dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName));
                })
                .AddDeveloperSigningCredential();
        }
        
        private WebApplicationBuilder ConfigureSerilog()
        {
            builder.Host.UseSerilog((context, lc) => lc
                .WriteTo.Console(
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(context.Configuration));
            
            return builder;
        }
        
        public WebApplication ConfigureServices()
        {
            builder.ConfigureSerilog();
            
            var identityServerBuilder = builder.ConfigureIdentityServer();
            
            builder.Services
                .AddExceptionHandler<GlobalExceptionHandler>()
                .AddHttpContextAccessor()
                .AddAuthBearer(builder.Configuration)
                .AddAuthPolicies(builder.Configuration)
                .AddInfrastructure(builder.Configuration)
                .AddApplication(builder.Configuration)
                .AddControllers();
            
            identityServerBuilder.AddAspNetIdentity<User>();
            
            return builder.Build();
        }
    }

    extension(WebApplication app)
    {
        public WebApplication ConfigurePipeline()
        {
            app.UseExceptionHandler();
            app.UseSerilogRequestLogging();

            app.UseIdentityServer();
            
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            
            return app;
        }
    }
    
    extension(IServiceCollection services)
    {
        private IServiceCollection AddAuthBearer(IConfiguration configuration)
        {
            var jwtOptions = configuration.ExtractOptions<JwtOptions>(JwtOptions.SectionName);
            
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = jwtOptions.Issuer;
                    options.Audience = jwtOptions.Audience;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,
                        ValidateLifetime = true
                    };

                    options.RequireHttpsMetadata = false;
                });
            
            return services;
        }
        
        private IServiceCollection AddAuthPolicies(IConfiguration configuration)
        {
            var emailRecoverApiKey = configuration.ExtractApiKey(ApiKeyOptions.PasswordRecover);

            services
                .AddSingleton<IAuthorizationHandler, ApiKeyHandler>()
                .AddScoped<IAuthorizationHandler, ProjectRoleHandler>();
            
            return services.AddAuthorization(options =>
            {
                options.AddPolicy(ProjectRoleRequirement.Viewer, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new ProjectRoleRequirement(UserClientRole.Viewer));
                });
        
                options.AddPolicy(ProjectRoleRequirement.Editor, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new ProjectRoleRequirement(UserClientRole.Editor));
                });
        
                options.AddPolicy(ProjectRoleRequirement.Admin, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new ProjectRoleRequirement(UserClientRole.Admin));
                });
        
                options.AddPolicy(ProjectRoleRequirement.Owner, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new ProjectRoleRequirement(UserClientRole.Owner));
                });
                
                options.AddPolicy(ApiKeyRequirement.EmailRecovery, policy 
                    => policy.Requirements.Add(new ApiKeyRequirement(emailRecoverApiKey)));
            });
        }
    }
    
    extension(IConfiguration configuration)
    {
        public string ExtractApiKey(string sections)
        {
            return configuration
                       .GetSection(ApiKeyRequirement.EmailRecovery)
                       .Get<ApiKeyOptions>()?
                       .Key
                   ?? throw new InvalidOperationException();
        }
    }
}
