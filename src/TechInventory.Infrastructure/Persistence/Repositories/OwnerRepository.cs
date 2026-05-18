using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Repositories;

public sealed class OwnerRepository(AppDbContext dbContext) : Repository<Owner, Guid>(dbContext), IOwnerRepository
{
    protected override IQueryable<Owner> DefaultQuery => DbContext.Owners.Where(owner => owner.IsActive);

    protected override IQueryable<Owner> AllQuery => DbContext.Owners;

    protected override string EntityName => nameof(Owner);

    protected override Guid GetKey(Owner entity) => entity.Id;

    public Task<Result<Owner>> AddAsync(Owner aggregate, CancellationToken cancellationToken)
        => AddEntityAsync(aggregate, cancellationToken);

    public async Task<Result<Owner>> GetByEntraObjectIdAsync(Guid entraObjectId, CancellationToken cancellationToken)
    {
        if (entraObjectId == Guid.Empty)
        {
            throw new ArgumentException("entraObjectId cannot be an empty GUID.", nameof(entraObjectId));
        }

        var owner = await AllQuery.SingleOrDefaultAsync(entity => entity.EntraObjectId == entraObjectId, cancellationToken).ConfigureAwait(false);
        return ToLookupResult(owner, entraObjectId);
    }

    public Task<Result<Owner>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetEntityByIdAsync(id, cancellationToken);

    public async Task<Result<Owner>> GetByNormalizedDisplayNameAsync(string normalizedDisplayName, CancellationToken cancellationToken)
    {
        var lookup = NormalizeLookupValue(normalizedDisplayName, nameof(normalizedDisplayName));
        var owner = await AllQuery.SingleOrDefaultAsync(entity => entity.DisplayName == lookup, cancellationToken).ConfigureAwait(false);
        return ToLookupResult(owner, lookup);
    }

    public Task<IReadOnlyList<Owner>> ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var query = (includeInactive ? AllQuery : DefaultQuery).AsNoTracking();
        return MergeTrackedAsync(
            query,
            entity => includeInactive || entity.IsActive,
            entities => entities.OrderBy(entity => entity.DisplayName, StringComparer.OrdinalIgnoreCase),
            cancellationToken);
    }

    public Task<Result<Owner>> UpdateAsync(Owner aggregate, CancellationToken cancellationToken)
        => UpdateEntityAsync(aggregate, cancellationToken);
}
