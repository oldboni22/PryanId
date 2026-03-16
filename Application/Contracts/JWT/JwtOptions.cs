namespace Application.Contracts.JWT;

public sealed record JwtOptions
{
    public const string SectionName = "JWT";
    
    public required string Issuer { get; init; }
    
    public required string Audience { get; init; }
    
    public required string TokenType { get; init; }
    
    public required string SelfTokenScope { get; init; }
    
    public int AccessExpiryMinutes { get; init; }
    
    public int RefreshExpiryDays { get; init; }
    
    public int MaxExpiredTokensStored { get; init; }
}