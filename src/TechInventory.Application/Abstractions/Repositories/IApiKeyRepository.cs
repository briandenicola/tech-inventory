using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Abstractions.Repositories;

public interface IApiKeyRepository : IAggregateRepository<ApiKey>
{
    /// <summary>
    /// Looks a key up by its public selector. Returns a failure result when no row
    /// matches — callers on the authentication path must still perform a dummy
    /// verifier comparison before failing (ADR 0003), so that "selector exists"
    /// and "selector does not exist" execute the same cryptographic work.
    /// </summary>
    Task<Result<ApiKey>> GetBySelectorAsync(string selector, CancellationToken cancellationToken);

    /// <summary>Paginated list of one principal's keys, newest first. Revoked and expired keys are included.</summary>
    Task<PagedResult<ApiKey>> GetByPrincipalAsync(
        ApiKeyPrincipalType principalType,
        Guid principalId,
        PageRequest pageRequest,
        CancellationToken cancellationToken);

    /// <summary>Paginated list of every key in the household, newest first — Admin emergency revocation.</summary>
    Task<PagedResult<ApiKey>> ListAllAsync(PageRequest pageRequest, CancellationToken cancellationToken);

    /// <summary>Count of the principal's keys that are neither revoked nor expired at <paramref name="asOf"/> — the quota check.</summary>
    Task<int> CountActiveByPrincipalAsync(
        ApiKeyPrincipalType principalType,
        Guid principalId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken);

    /// <summary>Whether the principal already has a key with this name (case-insensitive), ignoring revoked keys.</summary>
    Task<bool> NameExistsForPrincipalAsync(
        ApiKeyPrincipalType principalType,
        Guid principalId,
        string name,
        CancellationToken cancellationToken);
}
