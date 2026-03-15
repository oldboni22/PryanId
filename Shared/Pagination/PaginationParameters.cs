namespace Shared.Pagination;

public sealed record PaginationParameters(int PageSize = 15, int Page = 1)
{
    private const int MaxPageSize = 100;
    
    public static bool Validate(PaginationParameters? paginationParameters)
    {
        return paginationParameters is
        {
            PageSize: > 0 or <= MaxPageSize
        };
    }
}
