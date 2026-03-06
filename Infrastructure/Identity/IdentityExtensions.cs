using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Identity;

internal static class IdentityExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureIdentity()
        {
            services.AddIdentity<User, IdentityRole<Guid>>(options =>
                {
                    options
                        .ConfigurePassword()
                        .ConfigureUser();
                })
                .AddEntityFrameworkStores<UserDbContext>()
                .AddDefaultTokenProviders();

            return services;
        }
    }

    extension(IdentityOptions options)
    {
        private IdentityOptions ConfigurePassword()
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredUniqueChars = 0;
            
            return options;
        }
        
        private IdentityOptions ConfigureUser()
        {
            options.User.RequireUniqueEmail = true;
            
            return options;
        }
    }
}
