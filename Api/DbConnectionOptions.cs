namespace Api;

public record DbConnectionOptions
{
    private const string SectionName = "DbConnection";
    
    public const string ConfigurationalStorageSectionName = $"{SectionName}:Configurational";
    
    public const string OperationalStorageSectionName = $"{SectionName}:Operational";
    
    public required string ConnectionString { get; init; }
}
