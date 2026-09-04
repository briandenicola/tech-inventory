using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.ApiKeys;

/// <summary>
/// An API key as returned by list endpoints.
/// </summary>
/// <remarks>
/// Deliberately has no field for the secret or the verifier hash. The secret
/// exists only in <see cref="CreatedApiKeyResponse"/>, returned exactly once at
/// creation; the verifier hash is database-internal and never leaves the server.
/// Adding either to this record would leak key material into every list call.
/// </remarks>
public sealed record ApiKeyResponse(
    Guid Id,
    string Name,
    string Selector,
    ApiKeyScope Scope,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    bool IsActive,
    string? OwnerDisplayName = null)
{
    public static ApiKeyResponse FromEntity(ApiKey entity, DateTimeOffset asOf, string? ownerDisplayName = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ApiKeyResponse(
            entity.Id,
            entity.Name,
            entity.Selector,
            entity.Scope,
            entity.CreatedAt,
            entity.ExpiresAt,
            entity.RevokedAt,
            entity.IsActive(asOf),
            ownerDisplayName);
    }
}

/// <summary>
/// The one-time creation response. <see cref="Key"/> is the only moment the
/// plaintext credential exists outside the caller's own storage — it is not
/// persisted and cannot be retrieved again.
/// </summary>
public sealed record CreatedApiKeyResponse(
    Guid Id,
    string Name,
    string Selector,
    ApiKeyScope Scope,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    string Key);
