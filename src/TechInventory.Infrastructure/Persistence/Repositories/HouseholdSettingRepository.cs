using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Repositories;

public sealed class HouseholdSettingRepository(AppDbContext dbContext) : Repository<HouseholdSetting, Guid>(dbContext), IHouseholdSettingRepository
{
    protected override IQueryable<HouseholdSetting> DefaultQuery => DbContext.HouseholdSettings;

    protected override IQueryable<HouseholdSetting> AllQuery => DbContext.HouseholdSettings;

    protected override string EntityName => nameof(HouseholdSetting);

    protected override Guid GetKey(HouseholdSetting entity) => entity.Id;

    public Task<Result<HouseholdSetting>> AddAsync(HouseholdSetting aggregate, CancellationToken cancellationToken)
        => AddEntityAsync(aggregate, cancellationToken);

    public Task<Result<HouseholdSetting>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetEntityByIdAsync(id, cancellationToken);

    public async Task<Result<HouseholdSetting>> GetByHouseholdAndKeyAsync(Guid householdId, string key, CancellationToken cancellationToken)
    {
        if (householdId == Guid.Empty)
        {
            throw new ArgumentException("householdId cannot be an empty GUID.", nameof(householdId));
        }

        var lookup = NormalizeLookupValue(key, nameof(key));
        var setting = await AllQuery
            .SingleOrDefaultAsync(entity => entity.HouseholdId == householdId && entity.Key == lookup, cancellationToken)
            .ConfigureAwait(false);

        return ToLookupResult(setting, $"{householdId}:{lookup}");
    }

    public Task<IReadOnlyList<HouseholdSetting>> ListByHouseholdAsync(Guid householdId, CancellationToken cancellationToken)
    {
        if (householdId == Guid.Empty)
        {
            throw new ArgumentException("householdId cannot be an empty GUID.", nameof(householdId));
        }

        return MergeTrackedAsync(
            AllQuery.AsNoTracking().Where(entity => entity.HouseholdId == householdId),
            entity => entity.HouseholdId == householdId,
            entities => entities.OrderBy(entity => entity.Key, StringComparer.OrdinalIgnoreCase),
            cancellationToken);
    }

    public Task<Result<HouseholdSetting>> UpdateAsync(HouseholdSetting aggregate, CancellationToken cancellationToken)
        => UpdateEntityAsync(aggregate, cancellationToken);
}
