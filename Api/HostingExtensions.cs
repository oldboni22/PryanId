using System.Text.Json.Serialization;
using Application;
using Application.Auth;
using Application.Contracts.JWT;
using Application.Contracts.Options;
using Domain.Entities;
using Domain.Enums;
using Duende.IdentityServer;
using Duende.IdentityServer.Stores;
using Infrastructure;
using Infrastructure.Identity.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Shared;
using Shared.Db;
using Scalar.AspNetCore;

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
                .WriteTo.Console()
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(context.Configuration));
            
            return builder;
        }
        
        public WebApplication ConfigureServices()
        {
            builder.ConfigureSerilog();

            var identityServerBuilder = builder.ConfigureIdentityServer();

            builder.Services
                .AddOpenApi(options =>
                {
                    options.AddDocumentTransformer((document, context, cancellationToken) =>
                    {
                        document.Components ??= new OpenApiComponents();
                        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

                        var bearerScheme = new OpenApiSecurityScheme
                        {
                            Type = SecuritySchemeType.Http,
                            Scheme = "bearer",
                            BearerFormat = "JWT",
                            Description = "JWT Token"
                        };

                        document.Components.SecuritySchemes["Bearer"] = bearerScheme;

                        document.Security ??= new List<OpenApiSecurityRequirement>();

                        var requirement = new OpenApiSecurityRequirement();

                        var schemeReference = new OpenApiSecuritySchemeReference("Bearer", document);

                        requirement.Add(schemeReference, new List<string>());

                        document.Security.Add(requirement);

                        return Task.CompletedTask;
                    });
                })
                .AddProblemDetails()
                .AddExceptionHandler<GlobalExceptionHandler>()
                .AddHttpContextAccessor()
                .AddInfrastructure(builder.Configuration)
                .AddApplication(builder.Configuration);

            identityServerBuilder.AddAspNetIdentity<User>();
            
            builder.Services
                .AddAuthBearer(builder.Configuration)
                .AddAuthPolicies(builder.Configuration)
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            
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
            app.UseAuthorization();

            app.MapOpenApi();
            app.MapScalarApiReference();
            
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
                    options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
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
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var scopeClaim = context.Principal?.FindFirst("scope")?.Value;
                            
                            if (string.IsNullOrEmpty(scopeClaim) || !scopeClaim.Contains(jwtOptions.SelfTokenScope))
                            {
                                context.Fail($"Required scope is missing.");
                            }

                            context.Success();
                            await Task.CompletedTask;
                        }
                    };
                    
                    options.RequireHttpsMetadata = false;
                });
            
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IHttpContextAccessor>((options, accessor) =>
                {
                    options.TokenValidationParameters.IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
                    {
                        var requestServices = accessor.HttpContext?.RequestServices;
                        if (requestServices == null) return [];

                        var keyStore = requestServices.GetRequiredService<IValidationKeysStore>();
                        var keys = keyStore.GetValidationKeysAsync().GetAwaiter().GetResult();
                
                        return keys.Select(k => k.Key);
                    };
                });
            
            return services;
        }
        
        private IServiceCollection AddAuthPolicies(IConfiguration configuration)
        {
            var emailRecoverApiKey = configuration.ExtractApiKey(AppApiKeyOptions.PasswordRecover);

            services
                .AddSingleton<IAuthorizationHandler, ApiKeyHandler>()
                .AddScoped<IAuthorizationHandler, ProjectRoleHandler>();
            
            return services.AddAuthorization(options =>
            {
                options.AddPolicy(ClientRoleRequirement.Viewer, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new ClientRoleRequirement(UserClientRole.Viewer));
                });
        
                options.AddPolicy(ClientRoleRequirement.Editor, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new ClientRoleRequirement(UserClientRole.Editor));
                });
        
                options.AddPolicy(ClientRoleRequirement.Admin, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new ClientRoleRequirement(UserClientRole.Admin));
                });
        
                options.AddPolicy(ClientRoleRequirement.Owner, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new ClientRoleRequirement(UserClientRole.Owner));
                });
                
                options.AddPolicy(ApiKeyRequirement.EmailRecovery, policy 
                    => policy.Requirements.Add(new ApiKeyRequirement(emailRecoverApiKey)));
            });
        }
    }
    
    extension(IConfiguration configuration)
    {
        public string ExtractApiKey(string section)
        {
            return configuration
                       .GetSection(section)
                       .Get<AppApiKeyOptions>()?
                       .Key
                   ?? throw new InvalidOperationException();
        }
    }
}
