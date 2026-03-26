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

    DbSet<RefreshToken> RefreshTokens { get; }
    
    Task SaveChangesAsync(CancellationToken cancellationToken);
    
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
}
