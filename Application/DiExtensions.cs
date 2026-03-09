using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DiExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplication(IConfiguration configuration)
        {
            return services
                .AddScoped<IUserService, UserService>();
        }
    }
}
