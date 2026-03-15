namespace Shared.Pagination;

public sealed class PagedList<T>
{
    public List<T> Content { get; private set; } = [];

    public int PageSize { get; private set; }
    
    public int Page { get; private set; }

    public int TotalCount { get; private set; }

    public int TotalPages { get; private set; }

    public static PagedList<T> Create(IEnumerable<T> content, PaginationParameters paginationParameters, int totalItemCount)
    {
        return new PagedList<T>
        {
            Content = content.ToList(),
            Page =  paginationParameters.Page,
            PageSize =  paginationParameters.PageSize,
            TotalCount =  totalItemCount,
            TotalPages = (int)Math.Ceiling(totalItemCount / (double)paginationParameters.PageSize)
        };
    }
}
