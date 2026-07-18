using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Abstractions.Repositories;

public interface IDeviceRepository : IAggregateRepository<Device>
{
    Task<PagedResult<Device>> ListAsync(DeviceListCriteria criteria, CancellationToken cancellationToken);

    /// <summary>
    /// Fetches every device in <paramref name="ids"/> with a single round trip
    /// (favouring already-tracked instances), instead of one query per id.
    /// Ids that don't match any device are simply absent from the result.
    /// </summary>
    Task<IReadOnlyList<Device>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<Result<int>> ReassignBrandReferencesAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken);

    Task<Result<int>> ReassignCategoryReferencesAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken);

    Task<Result<int>> ReassignLocationReferencesAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken);

    Task<Result<int>> ReassignNetworkReferencesAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DeviceTag>> ListTagsAsync(Guid deviceId, CancellationToken cancellationToken);

    Task<Result<DeviceTag>> UpsertTagAsync(DeviceTag deviceTag, CancellationToken cancellationToken);

    Task<Result> RemoveTagAsync(Guid deviceId, Guid tagId, CancellationToken cancellationToken);
}
