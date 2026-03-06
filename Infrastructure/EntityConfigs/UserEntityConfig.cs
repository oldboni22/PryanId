using System.Security.Cryptography.Xml;
using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigs;

file static class Constraints
{
    public const int MaxNameLength = 15;
}

public class UserEntityConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);

        builder.Property(user => user.DisplayName)
            .HasMaxLength(Constraints.MaxNameLength)
            .IsRequired();
        
        builder.HasIndex(user => user.DisplayName)
            .IsUnique();
    }
}
