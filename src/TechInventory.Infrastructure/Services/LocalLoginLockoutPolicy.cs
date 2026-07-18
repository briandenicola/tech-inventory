using Microsoft.Extensions.Options;
using TechInventory.Application.Abstractions.Services;

namespace TechInventory.Infrastructure.Services;

/// <summary>
/// F025b — binds the lockout policy to <c>Auth:Local:*</c> alongside the rest
/// of the local-account configuration.
/// </summary>
public sealed class LocalLoginLockoutPolicy(IOptions<LocalJwtOptions> options) : ILocalLoginLockoutPolicy
{
    private readonly LocalJwtOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public int MaxFailedAttempts => _options.MaxFailedLoginAttempts;

    public TimeSpan LockoutDuration => TimeSpan.FromMinutes(_options.LockoutDurationMinutes);
}
