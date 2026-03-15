namespace Shared.Pagination;

public sealed class PagedList<T>
{
    private IEnumerable<T> Content { get; init; } = [];

    private int PageSize { get; init; }

    private int TotalCount { get; init; }

    private int TotalPages { get; init; }
}
