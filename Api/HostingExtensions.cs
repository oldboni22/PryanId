using Domain;
using Duende.IdentityServer;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
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
            var identityServerBuilder = builder.Services
                .AddIdentityServer(options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;

                    options.EmitStaticAudienceClaim = true;
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
                });

            builder.Services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    var connectionOptions =
                        builder.Configuration.ExtractOptions<GoogleAuthConnectionOptions>(
                            GoogleAuthConnectionOptions.SectionName);

                    options.ClientId = connectionOptions.ClientId;
                    options.ClientSecret = connectionOptions.ClientSecret;
                });

            builder.ConfigureSerilog();
            
            builder.Services
                .AddInfrastructure(builder.Configuration)
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