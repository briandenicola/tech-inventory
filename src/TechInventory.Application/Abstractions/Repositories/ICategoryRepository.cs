using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Abstractions.Repositories;

public interface ICategoryRepository : IAggregateRepository<Category>
{
    Task<IReadOnlyList<Category>> ListAsync(bool includeInactive, CancellationToken cancellationToken);

    Task<Result<Category>> GetByNameWithinParentAsync(string normalizedName, Guid? parentId, CancellationToken cancellationToken);
}
