namespace TechInventory.Application.Common.Paging;

public sealed record PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (totalCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCount), "TotalCount cannot be negative.");
        }

        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be at least 1.");
        }

        if (pageSize is < 1 or > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be between 1 and 200.");
        }

        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    public IReadOnlyList<T> Items { get; }

    public int TotalCount { get; }

    public int Page { get; }

    public int PageSize { get; }
}
