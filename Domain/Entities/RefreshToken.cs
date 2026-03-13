namespace Domain.Entities;

public sealed class RefreshToken
{
    public const int TokenSize = 44;
    
    public Guid Id { get; init; }
    
    public Guid UserId { get; init; }

    public User User { get; init; } = null!;
    
    public required string Token { get; init; }

    public DateTime ExpiresAt { get; init; }
    
    public DateTime CreatedAt { get; init; }
    
    public DateTime? RevokedAt { get; internal set; }

    
    public bool HasExpired(DateTime currentTime) => currentTime >= ExpiresAt;
    
    public bool IsActive(DateTime currentTime) => RevokedAt is null && !HasExpired(currentTime);
}
