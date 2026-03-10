using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Contracts.Db;

public interface IUserDbContext
{
    public DbSet<UserClient> UserClients { get; }
    
    public DbSet<User> Users { get; }
    
    public Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
