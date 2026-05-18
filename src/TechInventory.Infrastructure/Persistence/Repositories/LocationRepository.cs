using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Repositories;

public sealed class LocationRepository(AppDbContext dbContext) : Repository<Location, Guid>(dbContext), ILocationRepository
{
    protected override IQueryable<Location> DefaultQuery => DbContext.Locations.Where(location => location.IsActive);

    protected override IQueryable<Location> AllQuery => DbContext.Locations;

    protected override string EntityName => nameof(Location);

    protected override Guid GetKey(Location entity) => entity.Id;

    public Task<Result<Location>> AddAsync(Location aggregate, CancellationToken cancellationToken)
        => AddEntityAsync(aggregate, cancellationToken);

    public Task<Result<Location>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetEntityByIdAsync(id, cancellationToken);

    public async Task<Result<Location>> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken)
    {
        var lookup = NormalizeLookupValue(normalizedName, nameof(normalizedName));
        var location = await AllQuery.SingleOrDefaultAsync(entity => entity.Name == lookup, cancellationToken).ConfigureAwait(false);
        return ToLookupResult(location, lookup);
    }

    public Task<IReadOnlyList<Location>> ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var query = (includeInactive ? AllQuery : DefaultQuery).AsNoTracking();
        return MergeTrackedAsync(
            query,
            entity => includeInactive || entity.IsActive,
            entities => entities.OrderBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase),
            cancellationToken);
    }

    public Task<Result<Location>> UpdateAsync(Location aggregate, CancellationToken cancellationToken)
        => UpdateEntityAsync(aggregate, cancellationToken);
}
