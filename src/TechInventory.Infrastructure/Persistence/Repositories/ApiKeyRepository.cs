using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Infrastructure.Persistence.Repositories;

public sealed class ApiKeyRepository(AppDbContext dbContext) : Repository<ApiKey, Guid>(dbContext), IApiKeyRepository
{
    // Revoked and expired keys stay visible: the Settings list shows them so a
    // member can see what was revoked, and the auth path needs to find a revoked
    // key in order to reject it rather than reporting an unknown selector.
    protected override IQueryable<ApiKey> DefaultQuery => DbContext.ApiKeys;

    protected override IQueryable<ApiKey> AllQuery => DbContext.ApiKeys;

    protected override string EntityName => nameof(ApiKey);

    protected override Guid GetKey(ApiKey entity) => entity.Id;

    public Task<Result<ApiKey>> AddAsync(ApiKey aggregate, CancellationToken cancellationToken)
        => AddEntityAsync(aggregate, cancellationToken);

    public Task<Result<ApiKey>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => GetEntityByIdAsync(id, cancellationToken);

    public Task<Result<ApiKey>> UpdateAsync(ApiKey aggregate, CancellationToken cancellationToken)
        => UpdateEntityAsync(aggregate, cancellationToken);

    public async Task<Result<ApiKey>> GetBySelectorAsync(string selector, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            return Result<ApiKey>.Failure(Error.NotFound("API key not found."));
        }

        var tracked = FindTrackedEntity(key => string.Equals(key.Selector, selector, StringComparison.Ordinal));
        if (tracked is not null)
        {
            return Result<ApiKey>.Success(tracked);
        }

        var entity = await AllQuery
            .SingleOrDefaultAsync(key => key.Selector == selector, cancellationToken)
            .ConfigureAwait(false);

        return ToLookupResult(entity, selector);
    }

    public Task<PagedResult<ApiKey>> GetByPrincipalAsync(
        ApiKeyPrincipalType principalType,
        Guid principalId,
        PageRequest pageRequest,
        CancellationToken cancellationToken)
        => ToPagedResultAsync(
            AllQuery.Where(key => key.PrincipalType == principalType && key.PrincipalId == principalId),
            key => key.PrincipalType == principalType && key.PrincipalId == principalId,
            keys => keys.OrderByDescending(key => key.CreatedAt).ThenBy(key => key.Id),
            pageRequest,
            cancellationToken);

    public Task<PagedResult<ApiKey>> ListAllAsync(PageRequest pageRequest, CancellationToken cancellationToken)
        => ToPagedResultAsync(
            AllQuery,
            _ => true,
            keys => keys.OrderByDescending(key => key.CreatedAt).ThenBy(key => key.Id),
            pageRequest,
            cancellationToken);

    public async Task<int> CountActiveByPrincipalAsync(
        ApiKeyPrincipalType principalType,
        Guid principalId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        var persisted = await AllQuery
            .Where(key => key.PrincipalType == principalType
                && key.PrincipalId == principalId
                && key.RevokedAt == null
                && key.ExpiresAt > asOf)
            .Select(key => key.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Union with the change tracker so a key added earlier in the same scope
        // counts toward the quota — otherwise two creates in one unit of work
        // could both pass a check that only sees committed rows.
        var ids = new HashSet<Guid>(persisted);
        foreach (var local in Entities.Local.Where(key => key.PrincipalType == principalType
            && key.PrincipalId == principalId
            && key.IsActive(asOf)))
        {
            ids.Add(local.Id);
        }

        return ids.Count;
    }

    public async Task<bool> NameExistsForPrincipalAsync(
        ApiKeyPrincipalType principalType,
        Guid principalId,
        string name,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var trimmed = name.Trim();

        var existsLocally = Entities.Local.Any(key => key.PrincipalType == principalType
            && key.PrincipalId == principalId
            && key.RevokedAt == null
            && string.Equals(key.Name, trimmed, StringComparison.OrdinalIgnoreCase));

        if (existsLocally)
        {
            return true;
        }

        return await AllQuery
            .Where(key => key.PrincipalType == principalType
                && key.PrincipalId == principalId
                && key.RevokedAt == null)
            .AnyAsync(key => key.Name == trimmed, cancellationToken)
            .ConfigureAwait(false);
    }
}
