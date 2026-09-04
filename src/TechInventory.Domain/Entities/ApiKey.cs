using TechInventory.Domain.Enums;
using TechInventory.Domain.Primitives;

namespace TechInventory.Domain.Entities;

/// <summary>
/// #149 / ADR 0003 — a non-interactive credential that lets a household member
/// drive device inventory from scripts, cron, or a Home Assistant integration
/// without a browser sign-in.
/// </summary>
/// <remarks>
/// <para>
/// The credential is split: <see cref="Selector"/> is a public, indexed lookup
/// handle that is safe to log, and the secret half is never persisted at all —
/// only <see cref="VerifierHash"/>, an HMAC-SHA-256 of the secret under a
/// deployment pepper. A database breach therefore cannot yield a usable key.
/// The plaintext secret exists exactly once, in the creation response.
/// </para>
/// <para>
/// The aggregate stores <see cref="PrincipalType"/> + <see cref="PrincipalId"/>
/// rather than an <c>OwnerId</c> foreign key because a key may be issued by
/// either an <see cref="Owner"/> or a <see cref="LocalUser"/>, and those are
/// separate entities. It also stores <see cref="HouseholdId"/> explicitly:
/// <see cref="Owner"/> carries no household reference, so it cannot be derived.
/// </para>
/// <para>
/// The aggregate deliberately knows nothing about whether a key <em>should</em>
/// authenticate — that is a live decision made per request against the current
/// state of the principal (active? still Admin/Member?). <see cref="IsActive"/>
/// answers only the part the key itself owns: not revoked, not expired.
/// </para>
/// </remarks>
public sealed class ApiKey : AggregateRoot
{
    public const int MaxNameLength = 100;
    public const int MaxSelectorLength = 64;
    public const int MaxVerifierHashLength = 128;

    /// <summary>Active keys permitted per principal before creation is refused (ADR 0003).</summary>
    public const int MaxActiveKeysPerPrincipal = 5;

    /// <summary>Expiry applied when the caller does not choose one.</summary>
    public const int DefaultExpiryDays = 90;

    /// <summary>Longest permitted lifetime. Non-expiring keys are not permitted.</summary>
    public const int MaxExpiryDays = 365;

    public ApiKey(
        Guid id,
        Guid householdId,
        string name,
        string selector,
        string verifierHash,
        ApiKeyScope scope,
        ApiKeyPrincipalType principalType,
        Guid principalId,
        DateTimeOffset expiresAt,
        string? createdBy = null) : base(id)
    {
        HouseholdId = Guard.AgainstDefault(householdId, nameof(householdId));
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name), MaxNameLength);
        Selector = Guard.AgainstNullOrWhiteSpace(selector, nameof(selector), MaxSelectorLength);
        VerifierHash = Guard.AgainstNullOrWhiteSpace(verifierHash, nameof(verifierHash), MaxVerifierHashLength);
        Scope = scope;
        PrincipalType = principalType;
        PrincipalId = Guard.AgainstDefault(principalId, nameof(principalId));

        if (expiresAt <= CreatedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAt), "expiresAt must be in the future.");
        }

        ExpiresAt = expiresAt;
        CreatedBy = Guard.AgainstMaxLength(createdBy, nameof(createdBy), 256);
        ModifiedBy = CreatedBy;
    }

    public Guid HouseholdId { get; private set; }

    /// <summary>Human-chosen label, unique per principal (e.g. "Home Assistant").</summary>
    public string Name { get; private set; }

    /// <summary>
    /// Public lookup handle (base64url of 16 CSPRNG bytes). Safe to log and to
    /// return in list responses — it identifies a key but cannot authenticate one.
    /// </summary>
    public string Selector { get; private set; }

    /// <summary>
    /// HMAC-SHA-256 of the secret half under the deployment pepper. Database-internal:
    /// never returned by the API and never logged.
    /// </summary>
    public string VerifierHash { get; private set; }

    public ApiKeyScope Scope { get; private set; }

    public ApiKeyPrincipalType PrincipalType { get; private set; }

    /// <summary>Id of the <see cref="Owner"/> or <see cref="LocalUser"/> this key acts for.</summary>
    public Guid PrincipalId { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public string? RevokedBy { get; private set; }

    /// <summary>
    /// Whether the key itself is still usable at <paramref name="asOf"/>. Says nothing
    /// about the principal — the auth handler re-checks that live on every request.
    /// </summary>
    public bool IsActive(DateTimeOffset asOf) => RevokedAt is null && ExpiresAt > asOf;

    /// <summary>
    /// Revokes the key. Idempotent by design: revocation is an emergency action that
    /// may arrive twice (a retried request, an admin and the owner acting at once), and
    /// the second call must not fail or overwrite who revoked it first.
    /// </summary>
    public void Revoke(string? actor, DateTimeOffset? asOf = null)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = asOf ?? DateTimeOffset.UtcNow;
        RevokedBy = Guard.AgainstMaxLength(actor, nameof(actor), 256);
        Touch(actor);
    }
}
