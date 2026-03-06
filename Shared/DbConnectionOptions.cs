namespace Shared;

public record DbConnectionOptions
{
    public const string SectionName = "DbConnection";
    
    public required string ConnectionString { get; init; }
}
