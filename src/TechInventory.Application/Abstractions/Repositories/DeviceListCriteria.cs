using TechInventory.Application.Common.Paging;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Abstractions.Repositories;

public sealed record DeviceListCriteria
{
    public DeviceListCriteria(
        PageRequest pageRequest,
        string? search = null,
        Guid? brandId = null,
        Guid? categoryId = null,
        Guid? ownerId = null,
        Guid? locationId = null,
        Guid? networkId = null,
        DeviceStatus? status = null,
        bool includeAllStatuses = false,
        IReadOnlyCollection<Guid>? tagIds = null,
        DateOnly? purchasedAfter = null,
        DateOnly? purchasedBefore = null,
        string? sortBy = null,
        bool sortDescending = false)
    {
        if (purchasedAfter.HasValue && purchasedBefore.HasValue && purchasedAfter > purchasedBefore)
        {
            throw new ArgumentOutOfRangeException(nameof(purchasedAfter), "purchasedAfter cannot be later than purchasedBefore.");
        }

        PageRequest = pageRequest ?? throw new ArgumentNullException(nameof(pageRequest));
        Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        BrandId = NormalizeOptionalId(brandId, nameof(brandId));
        CategoryId = NormalizeOptionalId(categoryId, nameof(categoryId));
        OwnerId = NormalizeOptionalId(ownerId, nameof(ownerId));
        LocationId = NormalizeOptionalId(locationId, nameof(locationId));
        NetworkId = NormalizeOptionalId(networkId, nameof(networkId));
        Status = status;
        IncludeAllStatuses = includeAllStatuses;
        TagIds = tagIds?.Where(id => id != Guid.Empty).Distinct().ToArray() ?? [];
        PurchasedAfter = purchasedAfter;
        PurchasedBefore = purchasedBefore;
        SortBy = string.IsNullOrWhiteSpace(sortBy) ? null : sortBy.Trim();
        SortDescending = sortDescending;
    }

    public PageRequest PageRequest { get; }

    public string? Search { get; }

    public Guid? BrandId { get; }

    public Guid? CategoryId { get; }

    public Guid? OwnerId { get; }

    public Guid? LocationId { get; }

    public Guid? NetworkId { get; }

    public DeviceStatus? Status { get; }

    public bool IncludeAllStatuses { get; }

    public IReadOnlyCollection<Guid> TagIds { get; }

    public DateOnly? PurchasedAfter { get; }

    public DateOnly? PurchasedBefore { get; }

    public string? SortBy { get; }

    public bool SortDescending { get; }

    private static Guid? NormalizeOptionalId(Guid? value, string paramName)
    {
        if (value.HasValue && value.Value == Guid.Empty)
        {
            throw new ArgumentException($"{paramName} cannot be an empty GUID.", paramName);
        }

        return value;
    }
}
