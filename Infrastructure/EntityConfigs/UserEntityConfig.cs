using System.Security.Cryptography.Xml;
using Domain;
using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigs;

public sealed class UserEntityConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);

        builder.Property(user => user.DisplayName)
            .HasConversion(
                name => name.ToString(),
                str => DisplayName.FromDatabase(str))
            .HasMaxLength(DisplayName.MaxLength)
            .IsRequired();
        
        builder.HasIndex(user => user.DisplayName)
            .IsUnique();
    }
}
