using TechInventory.Domain.Entities;

namespace TechInventory.Application.Abstractions.Services;

/// <summary>
/// F025b — the brute-force lockout policy for local ("break-glass") accounts.
/// <see cref="LocalUser"/> can already record and enforce a lockout window;
/// this is the missing piece that decides *when* to arm one.
/// </summary>
public interface ILocalLoginLockoutPolicy
{
    /// <summary>Consecutive failed attempts allowed before an account is locked out.</summary>
    int MaxFailedAttempts { get; }

    /// <summary>How long an account stays locked out once <see cref="MaxFailedAttempts"/> is reached.</summary>
    TimeSpan LockoutDuration { get; }
}
