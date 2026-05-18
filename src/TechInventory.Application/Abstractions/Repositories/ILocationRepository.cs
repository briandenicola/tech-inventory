using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Abstractions.Repositories;

public interface ILocationRepository : IAggregateRepository<Location>
{
    Task<IReadOnlyList<Location>> ListAsync(bool includeInactive, CancellationToken cancellationToken);

    Task<Result<Location>> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken);
}
