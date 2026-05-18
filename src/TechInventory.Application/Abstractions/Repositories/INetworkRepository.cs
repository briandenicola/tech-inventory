using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Abstractions.Repositories;

public interface INetworkRepository : IAggregateRepository<Network>
{
    Task<IReadOnlyList<Network>> ListAsync(bool includeInactive, CancellationToken cancellationToken);

    Task<Result<Network>> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken);
}
