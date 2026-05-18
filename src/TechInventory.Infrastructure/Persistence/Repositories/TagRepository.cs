using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Repositories;

public sealed class TagRepository(AppDbContext dbContext) : Repository<Tag, Guid>(dbContext), ITagRepository
{
    protected override IQueryable<Tag> DefaultQuery => DbContext.Tags.Where(tag => tag.IsActive);

    protected override IQueryable<Tag> AllQuery => DbContext.Tags;

    protected override string EntityName => nameof(Tag);

    protected override Guid GetKey(Tag entity) => entity.Id;

    public Task<Result<Tag>> AddAsync(Tag aggregate, CancellationToken cancellationToken)
        => AddEntityAsync(aggregate, cancellationToken);

    public Task<Result<Tag>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetEntityByIdAsync(id, cancellationToken);

    public async Task<Result<Tag>> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken)
    {
        var lookup = NormalizeLookupValue(normalizedName, nameof(normalizedName));
        var tag = await AllQuery.SingleOrDefaultAsync(entity => entity.Name == lookup, cancellationToken).ConfigureAwait(false);
        return ToLookupResult(tag, lookup);
    }

    public Task<IReadOnlyList<Tag>> ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var query = (includeInactive ? AllQuery : DefaultQuery).AsNoTracking();
        return MergeTrackedAsync(
            query,
            entity => includeInactive || entity.IsActive,
            entities => entities.OrderBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase),
            cancellationToken);
    }

    public Task<Result<Tag>> UpdateAsync(Tag aggregate, CancellationToken cancellationToken)
        => UpdateEntityAsync(aggregate, cancellationToken);
}
