using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Repositories;

public sealed class HouseholdRepository(AppDbContext dbContext) : Repository<Household, Guid>(dbContext), IHouseholdRepository
{
    protected override IQueryable<Household> DefaultQuery => DbContext.Households;

    protected override IQueryable<Household> AllQuery => DbContext.Households;

    protected override string EntityName => nameof(Household);

    protected override Guid GetKey(Household entity) => entity.Id;

    public Task<Result<Household>> AddAsync(Household aggregate, CancellationToken cancellationToken)
        => AddEntityAsync(aggregate, cancellationToken);

    public Task<Result<Household>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetEntityByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Household>> ListAsync(CancellationToken cancellationToken)
    {
        return MergeTrackedAsync(
            AllQuery.AsNoTracking(),
            _ => true,
            entities => entities.OrderBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase),
            cancellationToken);
    }

    public Task<Result<Household>> UpdateAsync(Household aggregate, CancellationToken cancellationToken)
        => UpdateEntityAsync(aggregate, cancellationToken);
}
