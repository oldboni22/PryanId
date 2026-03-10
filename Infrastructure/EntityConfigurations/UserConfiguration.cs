using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);

        builder.Property(user => user.UserName)
            .HasMaxLength(25);
        
        builder.Property(user => user.NormalizedUserName)
            .HasMaxLength(25);

        builder.HasMany(user => user.UserClients);
        
        var navigation = builder.Metadata.FindNavigation(nameof(User.RefreshTokens));
        navigation!.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany<RefreshToken>(user => user.RefreshTokens, tokenBuilder =>
        {
            tokenBuilder.ToTable("RefreshTokens");
            tokenBuilder.HasKey(nameof(RefreshToken.Id));
            
            tokenBuilder.WithOwner().HasForeignKey(nameof(RefreshToken.UserId));
            
            tokenBuilder.Property(nameof(RefreshToken.Token))
                .HasMaxLength(RefreshToken.TokenSize)
                .IsFixedLength()
                .IsRequired();
            
            tokenBuilder.HasIndex(nameof(RefreshToken.Token))
                .IsUnique();
        });
    }
}
