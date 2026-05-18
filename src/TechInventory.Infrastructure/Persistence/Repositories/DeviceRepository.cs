using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Infrastructure.Persistence.Repositories;

public sealed class DeviceRepository(AppDbContext dbContext) : Repository<Device, Guid>(dbContext), IDeviceRepository
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

        IQueryable<Device> query = DbContext.Devices.AsNoTracking();

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

        return await ToPagedResultAsync(
            query,
            device => MatchesCriteria(device, criteria),
            devices => ApplyOrdering(devices, criteria.SortBy, criteria.SortDescending),
            criteria.PageRequest,
            cancellationToken).ConfigureAwait(false);
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
            return Result.Failure(new Error("NotFound", $"Device tag '{deviceId}:{tagId}' was not found."));
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
            return Result<DeviceTag>.Failure(new Error("NotFound", $"Device '{deviceTag.DeviceId}' was not found."));
        }

        var tagExists = await DbContext.Tags.AnyAsync(tag => tag.Id == deviceTag.TagId, cancellationToken).ConfigureAwait(false);
        if (!tagExists)
        {
            return Result<DeviceTag>.Failure(new Error("NotFound", $"Tag '{deviceTag.TagId}' was not found."));
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

    private static IOrderedEnumerable<Device> ApplyOrdering(IEnumerable<Device> devices, string? sortBy, bool sortDescending)
    {
        var normalizedSort = string.IsNullOrWhiteSpace(sortBy) ? "name" : sortBy.Trim().ToLowerInvariant();

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
}
