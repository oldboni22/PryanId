using Microsoft.Extensions.Configuration;

namespace Shared;

public static class DiExtensions
{
    extension(IConfiguration configuration)
    {
        public T ExtractOptions<T>(string sectionName) where T : class
        {
            var options = configuration
                              .GetSection(sectionName)
                              .Get<T>()
                          ?? throw new InvalidOperationException();

            return options;
        }
        
        public string ExtractConnectionString(string sectionName)
        {
            var options = configuration.ExtractOptions<DbConnectionOptions>(sectionName);
            
            return options.ConnectionString;
        }
    }
}