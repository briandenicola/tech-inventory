using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(AppDbContext dbContext) : Repository<Category, Guid>(dbContext), ICategoryRepository
{
    protected override IQueryable<Category> DefaultQuery => DbContext.Categories.Where(category => category.IsActive);

    protected override IQueryable<Category> AllQuery => DbContext.Categories;

    protected override string EntityName => nameof(Category);

    protected override Guid GetKey(Category entity) => entity.Id;

    public Task<Result<Category>> AddAsync(Category aggregate, CancellationToken cancellationToken)
        => AddEntityAsync(aggregate, cancellationToken);

    public async Task<Result<Category>> GetByNameWithinParentAsync(string normalizedName, Guid? parentId, CancellationToken cancellationToken)
    {
        var lookup = NormalizeLookupValue(normalizedName, nameof(normalizedName));
        var category = await AllQuery
            .SingleOrDefaultAsync(entity => entity.ParentId == parentId && entity.Name == lookup, cancellationToken)
            .ConfigureAwait(false);

        return ToLookupResult(category, $"{lookup}' within parent '{parentId?.ToString() ?? "root"}'");
    }

    public Task<Result<Category>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetEntityByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Category>> ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var query = (includeInactive ? AllQuery : DefaultQuery).AsNoTracking();
        return MergeTrackedAsync(
            query,
            entity => includeInactive || entity.IsActive,
            entities => entities
                .OrderBy(entity => entity.Depth)
                .ThenBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase),
            cancellationToken);
    }

    public Task<Result<Category>> UpdateAsync(Category aggregate, CancellationToken cancellationToken)
        => UpdateEntityAsync(aggregate, cancellationToken);
}
