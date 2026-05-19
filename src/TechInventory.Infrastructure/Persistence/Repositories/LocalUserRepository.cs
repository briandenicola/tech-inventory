using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Repositories;

public sealed class LocalUserRepository(AppDbContext dbContext) : Repository<LocalUser, Guid>(dbContext), ILocalUserRepository
{
    protected override IQueryable<LocalUser> DefaultQuery => DbContext.LocalUsers.Where(user => user.IsActive);

    protected override IQueryable<LocalUser> AllQuery => DbContext.LocalUsers;

    protected override string EntityName => nameof(LocalUser);

    protected override Guid GetKey(LocalUser entity) => entity.Id;

    public Task<Result<LocalUser>> AddAsync(LocalUser aggregate, CancellationToken cancellationToken)
        => AddEntityAsync(aggregate, cancellationToken);

    public Task<Result<LocalUser>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetEntityByIdAsync(id, cancellationToken);

    public async Task<Result<LocalUser>> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var normalized = LocalUser.NormalizeUsername(username);

        // Check the change tracker first so a seed/upsert performed earlier in
        // the same scope wins over a database round-trip.
        var tracked = FindTrackedEntity(user => string.Equals(user.Username, normalized, StringComparison.Ordinal));
        if (tracked is not null)
        {
            return Result<LocalUser>.Success(tracked);
        }

        var user = await AllQuery.SingleOrDefaultAsync(entity => entity.Username == normalized, cancellationToken).ConfigureAwait(false);
        return ToLookupResult(user, normalized);
    }

    public Task<Result<LocalUser>> UpdateAsync(LocalUser aggregate, CancellationToken cancellationToken)
        => UpdateEntityAsync(aggregate, cancellationToken);
}
