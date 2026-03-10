using Domain.Enums;

namespace Domain.Entities;

public sealed class UserClient
{
    public Guid UserId { get; init; }
    
    public required string ClientId { get; init; }
    
    public UserClientRole Role { get; set; }
}
