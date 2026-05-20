using TechInventory.Domain.Entities;

namespace TechInventory.Application.Abstractions.Services;

/// <summary>
/// F025 — issues short-lived access tokens for the local-credential fallback
/// path. Implementations sign with a key the API trusts (HS256 today) and stamp
/// claims that mirror the Entra shape so existing <c>[Authorize]</c> attributes
/// keep working.
/// </summary>
public interface ILocalTokenIssuer
{
    /// <summary>Token lifetime configured for this issuer (e.g. 8h).</summary>
    TimeSpan AccessTokenLifetime { get; }

    /// <summary>
    /// Issue a signed JWT for the given local user. Returns the encoded token +
    /// the absolute UTC expiry so the caller can hand both to the client.
    /// </summary>
    (string Token, DateTimeOffset ExpiresAtUtc) IssueAccessToken(LocalUser user);
}
