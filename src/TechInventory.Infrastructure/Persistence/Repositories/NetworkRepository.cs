using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Repositories;

public sealed class NetworkRepository(AppDbContext dbContext) : Repository<Network, Guid>(dbContext), INetworkRepository
{
    protected override IQueryable<Network> DefaultQuery => DbContext.Networks.Where(network => network.IsActive);

    protected override IQueryable<Network> AllQuery => DbContext.Networks;

    protected override string EntityName => nameof(Network);

    protected override Guid GetKey(Network entity) => entity.Id;

    public Task<Result<Network>> AddAsync(Network aggregate, CancellationToken cancellationToken)
        => AddEntityAsync(aggregate, cancellationToken);

    public Task<Result<Network>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetEntityByIdAsync(id, cancellationToken);

    public async Task<Result<Network>> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken)
    {
        var lookup = NormalizeLookupValue(normalizedName, nameof(normalizedName));
        var network = await AllQuery.SingleOrDefaultAsync(entity => entity.Name == lookup, cancellationToken).ConfigureAwait(false);
        return ToLookupResult(network, lookup);
    }

    public Task<IReadOnlyList<Network>> ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var query = (includeInactive ? AllQuery : DefaultQuery).AsNoTracking();
        return MergeTrackedAsync(
            query,
            entity => includeInactive || entity.IsActive,
            entities => entities.OrderBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase),
            cancellationToken);
    }

    public Task<Result<Network>> UpdateAsync(Network aggregate, CancellationToken cancellationToken)
        => UpdateEntityAsync(aggregate, cancellationToken);
}
