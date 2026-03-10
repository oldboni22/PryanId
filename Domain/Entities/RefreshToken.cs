namespace Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; init; }
    
    public Guid UserId { get; init; }
    
    public required string Token { get; init; }

    public DateTime ExpiresAt { get; init; }
    
    public DateTime CreatedAt { get; init; }
    
    public DateTime? RevokedAt { get; set; }

    
    public bool HasExpired(DateTime currentTime) => currentTime >= ExpiresAt;
    
    public bool IsActive(DateTime currentTime) => RevokedAt is null && !HasExpired(currentTime);
}
