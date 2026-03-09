namespace Shared.Db;

public sealed record DbConnectionOptions( string ConnectionString)
{
    public const string SectionName = "DbConnection";
}
