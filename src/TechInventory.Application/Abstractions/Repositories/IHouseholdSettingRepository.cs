using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Abstractions.Repositories;

public interface IHouseholdSettingRepository : IAggregateRepository<HouseholdSetting>
{
    Task<Result<HouseholdSetting>> GetByHouseholdAndKeyAsync(Guid householdId, string key, CancellationToken cancellationToken);

    Task<IReadOnlyList<HouseholdSetting>> ListByHouseholdAsync(Guid householdId, CancellationToken cancellationToken);
}
