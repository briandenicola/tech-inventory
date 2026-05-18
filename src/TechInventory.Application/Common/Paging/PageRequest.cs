namespace TechInventory.Application.Common.Paging;

public sealed record PageRequest
{
    public PageRequest(int page = 1, int pageSize = 25)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be at least 1.");
        }

        if (pageSize is < 1 or > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be between 1 and 200.");
        }

        Page = page;
        PageSize = pageSize;
    }

    public int Page { get; }

    public int PageSize { get; }
}
