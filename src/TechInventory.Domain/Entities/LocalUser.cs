using TechInventory.Domain.Enums;
using TechInventory.Domain.Primitives;

namespace TechInventory.Domain.Entities;

/// <summary>
/// F025 — break-glass local credential account that can authenticate when the
/// primary Entra ID issuer is unavailable. <see cref="LocalUser"/> is an
/// authentication concept and is intentionally separate from <see cref="Owner"/>
/// (a domain reference entity); the two may be linked informationally in a
/// future iteration.
/// </summary>
/// <remarks>
/// Password material lives only as an Argon2id hash + algorithm tag. The aggregate
/// exposes deliberate verbs (<see cref="SetPassword"/>, <see cref="RecordSuccessfulLogin"/>,
/// <see cref="RecordFailedLogin"/>, <see cref="ClearLockout"/>) so the handler layer
/// never assigns hash bytes by hand and the audit trail always reflects intent.
/// Lockout enforcement (rejecting logins while <see cref="LockoutUntilUtc"/> is in
/// the future) is enforced in the login handler, not in the aggregate, to keep
/// the entity persistence-safe and easy to test.
/// </remarks>
public sealed class LocalUser : AggregateRoot
{
    public const int MaxUsernameLength = 64;
    public const int MaxDisplayNameLength = 200;
    public const int MaxAlgorithmLength = 64;
    public const int MaxHashLength = 512;

    public LocalUser(
        Guid id,
        string username,
        string displayName,
        OwnerRole role,
        string passwordHash,
        string passwordAlgorithm,
        bool mustChangePasswordOnNextLogin = true,
        string? createdBy = null) : base(id)
    {
        Username = NormalizeUsername(username);
        DisplayName = Guard.AgainstNullOrWhiteSpace(displayName, nameof(displayName), MaxDisplayNameLength);
        Role = role;
        PasswordHash = Guard.AgainstNullOrWhiteSpace(passwordHash, nameof(passwordHash), MaxHashLength);
        PasswordAlgorithm = Guard.AgainstNullOrWhiteSpace(passwordAlgorithm, nameof(passwordAlgorithm), MaxAlgorithmLength);
        MustChangePasswordOnNextLogin = mustChangePasswordOnNextLogin;
        IsActive = true;
        LastPasswordChangeUtc = DateTimeOffset.UtcNow;
        CreatedBy = Guard.AgainstMaxLength(createdBy, nameof(createdBy), 256);
        ModifiedBy = CreatedBy;
    }

    public string Username { get; private set; }

    public string DisplayName { get; private set; }

    public OwnerRole Role { get; private set; }

    public string PasswordHash { get; private set; }

    public string PasswordAlgorithm { get; private set; }

    public bool MustChangePasswordOnNextLogin { get; private set; }

    public int FailedAttemptCount { get; private set; }

    public DateTimeOffset? LockoutUntilUtc { get; private set; }

    public DateTimeOffset? LastLoginUtc { get; private set; }

    public DateTimeOffset LastPasswordChangeUtc { get; private set; }

    public bool IsActive { get; private set; }

    public string NormalizedUsername => Username;

    public bool IsLockedOut(DateTimeOffset asOfUtc) =>
        LockoutUntilUtc.HasValue && LockoutUntilUtc.Value > asOfUtc;

    public void Rename(string displayName, string? modifiedBy = null)
    {
        DisplayName = Guard.AgainstNullOrWhiteSpace(displayName, nameof(displayName), MaxDisplayNameLength);
        Touch(modifiedBy);
    }

    public void SetRole(OwnerRole role, string? modifiedBy = null)
    {
        Role = role;
        Touch(modifiedBy);
    }

    /// <summary>
    /// Replaces the stored credential. Clears <see cref="MustChangePasswordOnNextLogin"/>
    /// when the change is user-driven (<paramref name="requireChangeOnNextLogin"/> = false)
    /// and sets it when the change is admin-issued.
    /// </summary>
    public void SetPassword(
        string passwordHash,
        string passwordAlgorithm,
        bool requireChangeOnNextLogin,
        string? modifiedBy = null)
    {
        PasswordHash = Guard.AgainstNullOrWhiteSpace(passwordHash, nameof(passwordHash), MaxHashLength);
        PasswordAlgorithm = Guard.AgainstNullOrWhiteSpace(passwordAlgorithm, nameof(passwordAlgorithm), MaxAlgorithmLength);
        MustChangePasswordOnNextLogin = requireChangeOnNextLogin;
        LastPasswordChangeUtc = DateTimeOffset.UtcNow;
        FailedAttemptCount = 0;
        LockoutUntilUtc = null;
        Touch(modifiedBy);
    }

    public void RecordSuccessfulLogin(DateTimeOffset whenUtc, string? modifiedBy = null)
    {
        LastLoginUtc = whenUtc;
        FailedAttemptCount = 0;
        LockoutUntilUtc = null;
        Touch(modifiedBy);
    }

    /// <summary>
    /// Increment the failed-attempt counter. The threshold and lockout window are
    /// policy decisions enforced in the handler layer (F025b); the aggregate
    /// merely records the count + a caller-supplied lockout expiry when relevant.
    /// </summary>
    public void RecordFailedLogin(DateTimeOffset? lockoutUntilUtc = null, string? modifiedBy = null)
    {
        FailedAttemptCount += 1;
        if (lockoutUntilUtc.HasValue)
        {
            LockoutUntilUtc = lockoutUntilUtc;
        }
        Touch(modifiedBy);
    }

    public void ClearLockout(string? modifiedBy = null)
    {
        FailedAttemptCount = 0;
        LockoutUntilUtc = null;
        Touch(modifiedBy);
    }

    public void Deactivate(string? modifiedBy = null)
    {
        IsActive = false;
        Touch(modifiedBy);
    }

    public void Reactivate(string? modifiedBy = null)
    {
        IsActive = true;
        Touch(modifiedBy);
    }

    /// <summary>
    /// Usernames are case-insensitive; we persist a lower-invariant copy so
    /// equality checks and the unique index work without collation surprises.
    /// </summary>
    public static string NormalizeUsername(string username)
    {
        var trimmed = Guard.AgainstNullOrWhiteSpace(username, nameof(username), MaxUsernameLength);
        if (trimmed.Length < 3)
        {
            throw new ArgumentException("Username must be at least 3 characters.", nameof(username));
        }
        return trimmed.ToLowerInvariant();
    }
}
