namespace Application.Contracts.JWT;

public sealed record JwtOptions
{
    public const string SectionName = "JWT";
    
    public required string Secret { get; init; }
    
    public required string Issuer { get; init; }
    
    public required  string Audience { get; init; }
    
    public int AccessExpiryMinutes { get; init; }
    
    public int RefreshExpiryDays { get; init; }
}