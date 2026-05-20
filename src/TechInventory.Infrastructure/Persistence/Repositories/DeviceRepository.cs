using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Exports;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Infrastructure.Persistence.Repositories;

public sealed class DeviceRepository(AppDbContext dbContext) : Repository<Device, Guid>(dbContext), IDeviceRepository, IDeviceExportService
{
    protected override IQueryable<Device> DefaultQuery => DbContext.Devices;

    protected override IQueryable<Device> AllQuery => DbContext.Devices;

    protected override string EntityName => nameof(Device);

    protected override Guid GetKey(Device entity) => entity.Id;

    public Task<Result<Device>> AddAsync(Device aggregate, CancellationToken cancellationToken)
        => AddEntityAsync(aggregate, cancellationToken);

    public Task<Result<Device>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetEntityByIdAsync(id, cancellationToken);

    public async Task<PagedResult<Device>> ListAsync(DeviceListCriteria criteria, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var query = ApplyFilters(DbContext.Devices.AsNoTracking(), criteria);

        return await ToPagedResultAsync(
            query,
            device => MatchesCriteria(device, criteria),
            devices => ApplyEnumerableOrdering(devices, criteria.SortBy, criteria.SortDescending),
            criteria.PageRequest,
            cancellationToken).ConfigureAwait(false);
    }

    public Task<Result<int>> ReassignBrandReferencesAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken)
        => ReassignReferencesAsync(device => device.BrandId == sourceId, device => device.ReassignBrand(targetId), cancellationToken);

    public Task<Result<int>> ReassignCategoryReferencesAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken)
        => ReassignReferencesAsync(device => device.CategoryId == sourceId, device => device.ReassignCategory(targetId), cancellationToken);

    public Task<Result<int>> ReassignLocationReferencesAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken)
        => ReassignReferencesAsync(device => device.LocationId == sourceId, device => device.ReassignLocation(targetId), cancellationToken);

    public Task<Result<int>> ReassignNetworkReferencesAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken)
        => ReassignReferencesAsync(device => device.NetworkId == sourceId, device => device.ReassignNetwork(targetId), cancellationToken);

    public async IAsyncEnumerable<DeviceExportRow> StreamExportAsync(DeviceListCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var devices = ApplyEnumerableOrdering(
                await ApplyFilters(DbContext.Devices.AsNoTracking(), criteria)
                    .ToListAsync()
                    .ConfigureAwait(false),
                criteria.SortBy,
                criteria.SortDescending)
            .ToArray();

        var brandIds = devices.Select(device => device.BrandId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToArray();
        var brands = await DbContext.Set<Brand>()
            .AsNoTracking()
            .Where(brand => brandIds.Contains(brand.Id))
            .ToDictionaryAsync(brand => brand.Id, brand => brand.Name)
            .ConfigureAwait(false);
        var categories = await DbContext.Categories.AsNoTracking().ToDictionaryAsync(category => category.Id, category => category.Name).ConfigureAwait(false);
        var owners = await DbContext.Owners.AsNoTracking().ToDictionaryAsync(owner => owner.Id, owner => owner.DisplayName).ConfigureAwait(false);
        var locations = await DbContext.Locations.AsNoTracking().ToDictionaryAsync(location => location.Id, location => location.Name).ConfigureAwait(false);
        var networks = await DbContext.Networks.AsNoTracking().ToDictionaryAsync(network => network.Id, network => network.Name).ConfigureAwait(false);

        foreach (var device in devices)
        {
            yield return new DeviceExportRow(
                device.Id,
                device.Name,
                device.BrandId.HasValue && brands.TryGetValue(device.BrandId.Value, out var brandName) ? brandName : null,
                categories[device.CategoryId],
                owners[device.OwnerId],
                locations[device.LocationId],
                device.NetworkId is { } networkId && networks.TryGetValue(networkId, out var networkName) ? networkName : null,
                device.Model,
                device.SerialNumber,
                device.PurchaseDate,
                device.PurchasePrice,
                device.Currency.Code,
                device.Status.ToString(),
                device.Notes,
                device.RetiredDate,
                device.DisposalMethod,
                device.CreatedAt,
                device.CreatedBy,
                device.ModifiedAt,
                device.ModifiedBy);
        }
    }

    public async Task<IReadOnlyList<DeviceTag>> ListTagsAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        if (deviceId == Guid.Empty)
        {
            throw new ArgumentException("deviceId cannot be an empty GUID.", nameof(deviceId));
        }

        var databaseItems = await DbContext.DeviceTags
            .AsNoTracking()
            .Where(deviceTag => deviceTag.DeviceId == deviceId && deviceTag.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return databaseItems
            .Concat(DbContext.DeviceTags.Local.Where(deviceTag => deviceTag.DeviceId == deviceId && deviceTag.IsActive))
            .GroupBy(deviceTag => new { deviceTag.DeviceId, deviceTag.TagId })
            .Select(group => group.Last())
            .OrderBy(deviceTag => deviceTag.TagId)
            .ToArray();
    }

    public async Task<Result> RemoveTagAsync(Guid deviceId, Guid tagId, CancellationToken cancellationToken)
    {
        if (deviceId == Guid.Empty)
        {
            throw new ArgumentException("deviceId cannot be an empty GUID.", nameof(deviceId));
        }

        if (tagId == Guid.Empty)
        {
            throw new ArgumentException("tagId cannot be an empty GUID.", nameof(tagId));
        }

        var deviceTag = DbContext.DeviceTags.Local
            .FirstOrDefault(entity => entity.DeviceId == deviceId && entity.TagId == tagId && entity.IsActive)
            ?? await DbContext.DeviceTags
                .SingleOrDefaultAsync(entity => entity.DeviceId == deviceId && entity.TagId == tagId && entity.IsActive, cancellationToken)
                .ConfigureAwait(false);

        if (deviceTag is null)
        {
            return Result.Failure(Error.NotFound($"Device tag '{deviceId}:{tagId}' was not found."));
        }

        deviceTag.Deactivate();
        return Result.Success();
    }

    public async Task<Result<DeviceTag>> UpsertTagAsync(DeviceTag deviceTag, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(deviceTag);

        var deviceExists = await DbContext.Devices.AnyAsync(device => device.Id == deviceTag.DeviceId, cancellationToken).ConfigureAwait(false);
        if (!deviceExists)
        {
            return Result<DeviceTag>.Failure(Error.NotFound($"Device '{deviceTag.DeviceId}' was not found."));
        }

        var tagExists = await DbContext.Tags.AnyAsync(tag => tag.Id == deviceTag.TagId, cancellationToken).ConfigureAwait(false);
        if (!tagExists)
        {
            return Result<DeviceTag>.Failure(Error.NotFound($"Tag '{deviceTag.TagId}' was not found."));
        }

        var existing = DbContext.DeviceTags.Local
            .FirstOrDefault(entity => entity.DeviceId == deviceTag.DeviceId && entity.TagId == deviceTag.TagId)
            ?? await DbContext.DeviceTags
                .SingleOrDefaultAsync(
                    entity => entity.DeviceId == deviceTag.DeviceId && entity.TagId == deviceTag.TagId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (existing is null)
        {
            await DbContext.DeviceTags.AddAsync(deviceTag, cancellationToken).ConfigureAwait(false);
            return Result<DeviceTag>.Success(deviceTag);
        }

        if (!existing.IsActive)
        {
            existing.Reactivate();
        }

        return Result<DeviceTag>.Success(existing);
    }

    public Task<Result<Device>> UpdateAsync(Device aggregate, CancellationToken cancellationToken)
        => UpdateEntityAsync(aggregate, cancellationToken);

    private IQueryable<Device> ApplyFilters(IQueryable<Device> query, DeviceListCriteria criteria)
    {
        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var search = criteria.Search.Trim();
            query = query.Where(device =>
                device.Name.Contains(search) ||
                (device.Model != null && device.Model.Contains(search)) ||
                (device.SerialNumber != null && device.SerialNumber.Contains(search)) ||
                (device.Notes != null && device.Notes.Contains(search)));
        }

        if (criteria.BrandId.HasValue)
        {
            query = query.Where(device => device.BrandId == criteria.BrandId.Value);
        }

        if (criteria.CategoryId.HasValue)
        {
            query = query.Where(device => device.CategoryId == criteria.CategoryId.Value);
        }

        if (criteria.OwnerId.HasValue)
        {
            query = query.Where(device => device.OwnerId == criteria.OwnerId.Value);
        }

        if (criteria.LocationId.HasValue)
        {
            query = query.Where(device => device.LocationId == criteria.LocationId.Value);
        }

        if (criteria.NetworkId.HasValue)
        {
            query = query.Where(device => device.NetworkId == criteria.NetworkId.Value);
        }

        if (criteria.Status.HasValue)
        {
            query = query.Where(device => device.Status == criteria.Status.Value);
        }
        else
        {
            query = query.Where(device => device.Status != DeviceStatus.Disposed);
        }

        if (criteria.PurchasedAfter.HasValue)
        {
            query = query.Where(device => device.PurchaseDate >= criteria.PurchasedAfter.Value);
        }

        if (criteria.PurchasedBefore.HasValue)
        {
            query = query.Where(device => device.PurchaseDate <= criteria.PurchasedBefore.Value);
        }

        if (criteria.TagIds.Count > 0)
        {
            var tagIds = criteria.TagIds.ToArray();
            query = query.Where(device => DbContext.DeviceTags
                .Where(deviceTag => deviceTag.DeviceId == device.Id && deviceTag.IsActive && tagIds.Contains(deviceTag.TagId))
                .Select(deviceTag => deviceTag.TagId)
                .Distinct()
                .Count() == tagIds.Length);
        }

        return query;
    }

    private async Task<Result<int>> ReassignReferencesAsync(
        System.Linq.Expressions.Expression<Func<Device, bool>> predicate,
        Action<Device> applyChange,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(applyChange);

        var devices = await DbContext.Devices
            .Where(predicate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var device in devices)
        {
            applyChange(device);
        }

        return Result<int>.Success(devices.Count);
    }

    private static IOrderedEnumerable<Device> ApplyEnumerableOrdering(IEnumerable<Device> devices, string? sortBy, bool sortDescending)
    {
        var normalizedSort = NormalizeSort(sortBy);

        return normalizedSort switch
        {
            "purchasedate" => sortDescending
                ? devices.OrderByDescending(device => device.PurchaseDate).ThenByDescending(device => device.Name, StringComparer.OrdinalIgnoreCase)
                : devices.OrderBy(device => device.PurchaseDate).ThenBy(device => device.Name, StringComparer.OrdinalIgnoreCase),
            "createdat" => sortDescending
                ? devices.OrderByDescending(device => device.CreatedAt).ThenByDescending(device => device.Name, StringComparer.OrdinalIgnoreCase)
                : devices.OrderBy(device => device.CreatedAt).ThenBy(device => device.Name, StringComparer.OrdinalIgnoreCase),
            _ => sortDescending
                ? devices.OrderByDescending(device => device.Name, StringComparer.OrdinalIgnoreCase).ThenByDescending(device => device.CreatedAt)
                : devices.OrderBy(device => device.Name, StringComparer.OrdinalIgnoreCase).ThenBy(device => device.CreatedAt)
        };
    }

    private static IOrderedQueryable<Device> ApplyQueryableOrdering(IQueryable<Device> devices, string? sortBy, bool sortDescending)
    {
        var normalizedSort = NormalizeSort(sortBy);

        return normalizedSort switch
        {
            "purchasedate" => sortDescending
                ? devices.OrderByDescending(device => device.PurchaseDate).ThenByDescending(device => device.Name).ThenByDescending(device => device.Id)
                : devices.OrderBy(device => device.PurchaseDate).ThenBy(device => device.Name).ThenBy(device => device.Id),
            "createdat" => sortDescending
                ? devices.OrderByDescending(device => device.CreatedAt.UtcDateTime).ThenByDescending(device => device.Name).ThenByDescending(device => device.Id)
                : devices.OrderBy(device => device.CreatedAt.UtcDateTime).ThenBy(device => device.Name).ThenBy(device => device.Id),
            _ => sortDescending
                ? devices.OrderByDescending(device => device.Name).ThenByDescending(device => device.Id)
                : devices.OrderBy(device => device.Name).ThenBy(device => device.Id)
        };
    }

    private bool MatchesCriteria(Device device, DeviceListCriteria criteria)
    {
        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var search = criteria.Search.Trim();
            var matchesSearch = device.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (device.Model?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (device.SerialNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (device.Notes?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);
            if (!matchesSearch)
            {
                return false;
            }
        }

        if (criteria.BrandId.HasValue && device.BrandId != criteria.BrandId.Value)
        {
            return false;
        }

        if (criteria.CategoryId.HasValue && device.CategoryId != criteria.CategoryId.Value)
        {
            return false;
        }

        if (criteria.OwnerId.HasValue && device.OwnerId != criteria.OwnerId.Value)
        {
            return false;
        }

        if (criteria.LocationId.HasValue && device.LocationId != criteria.LocationId.Value)
        {
            return false;
        }

        if (criteria.NetworkId.HasValue && device.NetworkId != criteria.NetworkId.Value)
        {
            return false;
        }

        if (criteria.Status.HasValue)
        {
            if (device.Status != criteria.Status.Value)
            {
                return false;
            }
        }
        else if (device.Status == DeviceStatus.Disposed)
        {
            return false;
        }

        if (criteria.PurchasedAfter.HasValue && device.PurchaseDate < criteria.PurchasedAfter.Value)
        {
            return false;
        }

        if (criteria.PurchasedBefore.HasValue && device.PurchaseDate > criteria.PurchasedBefore.Value)
        {
            return false;
        }

        if (criteria.TagIds.Count == 0)
        {
            return true;
        }

        var activeTagIds = DbContext.DeviceTags.Local
            .Where(deviceTag => deviceTag.DeviceId == device.Id && deviceTag.IsActive)
            .Select(deviceTag => deviceTag.TagId)
            .Distinct()
            .ToHashSet();

        return criteria.TagIds.All(activeTagIds.Contains);
    }

    private static string NormalizeSort(string? sortBy)
        => string.IsNullOrWhiteSpace(sortBy) ? "name" : sortBy.Trim().ToLowerInvariant();
}
