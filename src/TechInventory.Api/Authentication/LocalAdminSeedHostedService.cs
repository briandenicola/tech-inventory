using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TechInventory.Application.Abstractions.Services;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.Infrastructure.Persistence;
using TechInventory.Infrastructure.Services;

namespace TechInventory.Api.Authentication;

/// <summary>
/// F025 — break-glass seeding of a local Admin account.
///
/// Runs once at startup. When <c>Auth:Local:SeedEnabled=true</c> AND
/// <c>SeedUsername</c>/<c>SeedPassword</c> are populated, ensures a local
/// Admin row exists with the given credentials and
/// <c>MustChangePasswordOnNextLogin = true</c>.
///
/// Refuses to start in Production unless <c>SeedAllowInProd=true</c> so that a
/// leaked Compose file can't accidentally seed an admin on prod hardware.
/// Logs a CRITICAL warning every startup while seeding is configured — it is
/// an operator's job to clear the env vars after the first successful login.
/// </summary>
public sealed class LocalAdminSeedHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<LocalJwtOptions> options,
    IHostEnvironment environment,
    ILogger<LocalAdminSeedHostedService> logger) : IHostedService
{
    private readonly LocalJwtOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.SeedEnabled)
        {
            return;
        }

        if (environment.IsProduction() && !_options.SeedAllowInProd)
        {
            throw new InvalidOperationException(
                "Auth:Local:SeedEnabled is true in Production without Auth:Local:SeedAllowInProd. Refusing to start.");
        }

        var username = _options.SeedUsername;
        var password = _options.SeedPassword;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Auth:Local:SeedEnabled=true but SeedUsername/SeedPassword are not set; skipping local admin seed.");
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var normalizedUsername = LocalUser.NormalizeUsername(username);
        var existing = await dbContext.LocalUsers
            .FirstOrDefaultAsync(user => user.Username == normalizedUsername, cancellationToken)
            .ConfigureAwait(false);

        var hash = hasher.Hash(password);
        if (existing is null)
        {
            var seeded = new LocalUser(
                Guid.NewGuid(),
                normalizedUsername,
                displayName: $"Local admin ({normalizedUsername})",
                role: OwnerRole.Admin,
                passwordHash: hash,
                passwordAlgorithm: hasher.CurrentAlgorithm,
                mustChangePasswordOnNextLogin: true,
                createdBy: "seed:LocalAdminSeedHostedService");
            await dbContext.LocalUsers.AddAsync(seeded, cancellationToken).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogCritical("[F025] Seeded local Admin account '{Username}' (must change password on first login). Clear Auth:Local:Seed* env vars after first successful login.", normalizedUsername);
        }
        else
        {
            // Idempotent: re-hash + force must-change so a rerun acts as a
            // password reset for the break-glass account.
            existing.SetPassword(hash, hasher.CurrentAlgorithm, requireChangeOnNextLogin: true, modifiedBy: "seed:LocalAdminSeedHostedService");
            existing.Reactivate("seed:LocalAdminSeedHostedService");
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogCritical("[F025] Reset local Admin '{Username}' from seed config (must change password on next login). Clear Auth:Local:Seed* env vars after first successful login.", normalizedUsername);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
