using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Contracts.Db;

public interface IUserDbContext
{
    DbSet<UserClient> UserClients { get; }
    
    DbSet<User> Users { get; }
    
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    
    DbSet<RefreshToken> RefreshTokens { get; }
}
