using Domain.Entities;
using Application.Contracts.Db;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public sealed class UserDbContext(DbContextOptions options) 
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options), IUserDbContext
{
    public DbSet<UserClient> UserClients { get; init; }
    
    Task IUserDbContext.SaveChangesAsync(CancellationToken cancellationToken)
    {
        return SaveChangesAsync(true, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(UserDbContext).Assembly);
    }
}
