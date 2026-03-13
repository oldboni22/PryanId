using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(token => token.Id);
            
        builder.Property(token => token.Token)
            .HasMaxLength(RefreshToken.TokenSize)
            .IsFixedLength()
            .IsRequired();
            
        builder.HasIndex(token => token.Token)
            .IsUnique();
    }
}
