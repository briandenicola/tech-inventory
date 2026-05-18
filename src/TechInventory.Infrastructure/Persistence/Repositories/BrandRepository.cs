using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Repositories;

public sealed class BrandRepository(AppDbContext dbContext) : Repository<Brand, Guid>(dbContext), IBrandRepository
{
    protected override IQueryable<Brand> DefaultQuery => DbContext.Brands.Where(brand => brand.IsActive);

    protected override IQueryable<Brand> AllQuery => DbContext.Brands;

    protected override string EntityName => nameof(Brand);

    protected override Guid GetKey(Brand entity) => entity.Id;

    public Task<Result<Brand>> AddAsync(Brand aggregate, CancellationToken cancellationToken)
        => AddEntityAsync(aggregate, cancellationToken);

    public Task<Result<Brand>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetEntityByIdAsync(id, cancellationToken);

    public async Task<Result<Brand>> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken)
    {
        var lookup = NormalizeLookupValue(normalizedName, nameof(normalizedName));
        var brand = await AllQuery.SingleOrDefaultAsync(entity => entity.Name == lookup, cancellationToken).ConfigureAwait(false);
        return ToLookupResult(brand, lookup);
    }

    public Task<IReadOnlyList<Brand>> ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var query = (includeInactive ? AllQuery : DefaultQuery).AsNoTracking();
        return MergeTrackedAsync(
            query,
            entity => includeInactive || entity.IsActive,
            entities => entities.OrderBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase),
            cancellationToken);
    }

    public Task<Result<Brand>> UpdateAsync(Brand aggregate, CancellationToken cancellationToken)
        => UpdateEntityAsync(aggregate, cancellationToken);
}
