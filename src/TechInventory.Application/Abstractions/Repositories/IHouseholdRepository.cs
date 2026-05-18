using TechInventory.Domain.Entities;

namespace TechInventory.Application.Abstractions.Repositories;

public interface IHouseholdRepository : IAggregateRepository<Household>
{
    Task<IReadOnlyList<Household>> ListAsync(CancellationToken cancellationToken);
}
