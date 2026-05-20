using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Abstractions.Repositories;

/// <summary>
/// F025 — persistence boundary for the local-credential break-glass accounts.
/// Lookup is by username-or-id; listing/paging is intentionally deferred until
/// the admin UI ships in F025b.
/// </summary>
public interface ILocalUserRepository : IAggregateRepository<LocalUser>
{
    /// <summary>
    /// Resolve by case-insensitive normalized username. Returns
    /// <see cref="Result{T}"/> failure when no row matches; callers should treat
    /// "no row" as the same outcome as "wrong password" to avoid username
    /// enumeration via timing.
    /// </summary>
    Task<Result<LocalUser>> GetByUsernameAsync(string username, CancellationToken cancellationToken);
}
