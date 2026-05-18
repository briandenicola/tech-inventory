using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Abstractions.Repositories;

public interface IOwnerRepository : IAggregateRepository<Owner>
{
    Task<IReadOnlyList<Owner>> ListAsync(bool includeInactive, CancellationToken cancellationToken);

    Task<Result<Owner>> GetByNormalizedDisplayNameAsync(string normalizedDisplayName, CancellationToken cancellationToken);

    Task<Result<Owner>> GetByEntraObjectIdAsync(Guid entraObjectId, CancellationToken cancellationToken);
}
