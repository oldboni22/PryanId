namespace Api;

public class GoogleAuthConnectionOptions
{
    public const string SectionName = "GoogleAuth";
    
    public required string ClientId { get; init; }
    
    public required string ClientSecret { get; init; }
}