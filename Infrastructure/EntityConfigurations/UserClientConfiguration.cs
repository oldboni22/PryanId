using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public sealed class UserClientConfiguration : IEntityTypeConfiguration<UserClient>
{
    public void Configure(EntityTypeBuilder<UserClient> builder)
    {
        builder.HasKey(x => new { x.UserId, x.ClientId });
        
        builder
            .HasOne<User>()             
            .WithMany(u => u.UserClients)
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
