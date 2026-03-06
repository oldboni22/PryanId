namespace Shared.Db;

public sealed record DbConnectionOptions
{
    public const string SectionName = "DbConnection";
    
    public required string ConnectionString { get; init; }
}
