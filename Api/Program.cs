using Api;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Infrastructure;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Serilog;
using Shared;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);
    
    var app = builder
        .ConfigureServices()
        .ConfigurePipeline();

    app.MigrateDatabases(typeof(UserDbContext), typeof(ConfigurationDbContext), typeof(PersistedGrantDbContext));
    
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}