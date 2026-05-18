using TechInventory.Application.Common.Paging;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Abstractions.Repositories;

public sealed record ImportBatchListCriteria
{
    public ImportBatchListCriteria(PageRequest pageRequest, ImportStatus? status = null, DateTimeOffset? createdAfter = null, DateTimeOffset? createdBefore = null)
    {
        if (createdAfter.HasValue && createdBefore.HasValue && createdAfter > createdBefore)
        {
            throw new ArgumentOutOfRangeException(nameof(createdAfter), "createdAfter cannot be later than createdBefore.");
        }

        PageRequest = pageRequest ?? throw new ArgumentNullException(nameof(pageRequest));
        Status = status;
        CreatedAfter = createdAfter;
        CreatedBefore = createdBefore;
    }

    public PageRequest PageRequest { get; }

    public ImportStatus? Status { get; }

    public DateTimeOffset? CreatedAfter { get; }

    public DateTimeOffset? CreatedBefore { get; }
}
