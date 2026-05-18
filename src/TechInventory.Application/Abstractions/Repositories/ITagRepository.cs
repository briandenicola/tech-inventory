using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Abstractions.Repositories;

public interface ITagRepository : IAggregateRepository<Tag>
{
    Task<IReadOnlyList<Tag>> ListAsync(bool includeInactive, CancellationToken cancellationToken);

    Task<Result<Tag>> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken);
}
