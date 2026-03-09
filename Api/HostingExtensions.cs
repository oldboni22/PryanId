using Application;
using Application.Auth;
using Domain;
using Domain.Entities;
using Domain.Enums;
using Duende.IdentityServer;
using Infrastructure;
using Infrastructure.Application.JWT;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
            builder.Services.AddHttpContextAccessor();
           
            var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                             ?? throw new InvalidOperationException();
            
            var identityServerBuilder = builder.Services
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
            
            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = jwtOptions.Issuer;
                    options.Audience =  jwtOptions.Audience;
                    
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,
                        ValidateLifetime = true
                    };
                    
                    options.RequireHttpsMetadata = false;
                })
                .AddGoogle(options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    var connectionOptions =
                        builder.Configuration.ExtractOptions<GoogleAuthConnectionOptions>(
                            GoogleAuthConnectionOptions.SectionName);

                    options.ClientId = connectionOptions.ClientId;
                    options.ClientSecret = connectionOptions.ClientSecret;
                });
            
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(ProjectRoleRequirement.Viewer, policy => 
                    policy.Requirements.Add(new ProjectRoleRequirement(UserClientRole.Viewer)));
        
                options.AddPolicy(ProjectRoleRequirement.Editor, policy => 
                    policy.Requirements.Add(new ProjectRoleRequirement(UserClientRole.Editor)));
        
                options.AddPolicy(ProjectRoleRequirement.Admin, policy => 
                    policy.Requirements.Add(new ProjectRoleRequirement(UserClientRole.Admin)));
        
                options.AddPolicy(ProjectRoleRequirement.Owner, policy => 
                    policy.Requirements.Add(new ProjectRoleRequirement(UserClientRole.Owner)));
            });
            
            builder.Services
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
            app.UseSerilogRequestLogging();

            app.UseIdentityServer();
            app.UseAuthorization();

            app.MapControllers();
            
            return app;
        }
    }
}
