namespace Application.Contracts.Db;

/// <summary>
/// We need that since there are different ways to perform bulk delete in different db's hence ef core providers!
/// </summary>
public interface IBulkDeleteClientRelationsHelper
{
    Task ExecuteAsync(string clientId, int bulk);
}
