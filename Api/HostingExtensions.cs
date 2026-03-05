using Duende.IdentityServer;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.Configuration;
using Serilog;

namespace Api;

internal static class HostingExtensions
{
    extension(ConfigurationManager configuration)
    {
        private T ExtractOptions<T>(string sectionName) where T : class
        {
            var options = configuration
                    .GetSection(sectionName)
                    .Get<T>()
                    ?? throw new InvalidConfigurationException();

            return options;
        }
        
        private string GetConnectionString(string sectionName)
        {
            var options = configuration.ExtractOptions<DbConnectionOptions>(sectionName);
            
            return options.ConnectionString;
        }
    }
    
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder ConfigureSerilog()
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
                        builder.Configuration.GetConnectionString(DbConnectionOptions.ConfigurationalStorageSectionName);
                    
                    options.ConfigureDbContext = b => b
                        .UseNpgsql(connectionString, dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName));
                })
                .AddConfigurationStoreCache()
                .AddOperationalStore(options =>
                {
                    var connectionString =
                        builder.Configuration.GetConnectionString(DbConnectionOptions.OperationalStorageSectionName);

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

            builder.Services.AddControllers();
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

            return app;
        }
    }
}