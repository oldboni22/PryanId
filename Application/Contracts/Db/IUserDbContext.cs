using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Application.Contracts.Db;

public interface IUserDbContext
{
    DbSet<UserClient> UserClients { get; }
    
    DbSet<User> Users { get; }
    
    Task SaveChangesAsync(CancellationToken cancellationToken);
    
    DbSet<RefreshToken> RefreshTokens { get; }
    
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
}
