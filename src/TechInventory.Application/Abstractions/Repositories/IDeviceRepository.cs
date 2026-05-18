using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Abstractions.Repositories;

public interface IDeviceRepository : IAggregateRepository<Device>
{
    Task<PagedResult<Device>> ListAsync(DeviceListCriteria criteria, CancellationToken cancellationToken);

    Task<IReadOnlyList<DeviceTag>> ListTagsAsync(Guid deviceId, CancellationToken cancellationToken);

    Task<Result<DeviceTag>> UpsertTagAsync(DeviceTag deviceTag, CancellationToken cancellationToken);

    Task<Result> RemoveTagAsync(Guid deviceId, Guid tagId, CancellationToken cancellationToken);
}
