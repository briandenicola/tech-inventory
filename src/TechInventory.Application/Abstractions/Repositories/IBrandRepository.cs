using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Abstractions.Repositories;

public interface IBrandRepository : IAggregateRepository<Brand>
{
    Task<IReadOnlyList<Brand>> ListAsync(bool includeInactive, CancellationToken cancellationToken);

    Task<Result<Brand>> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken);
}
