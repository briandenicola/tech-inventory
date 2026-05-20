using MediatR;
using Microsoft.Extensions.Logging;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Abstractions.Services;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Auth.Commands;

/// <summary>
/// F025 — local-credential sign-in.
///
/// Validates username + password against the stored Argon2id hash. On success
/// issues a short-lived JWT (claims mirror the Entra shape so existing
/// <c>[Authorize(Roles=...)]</c> attributes Just Work), bumps
/// <c>LastLoginUtc</c>, and clears any failed-attempt counter.
///
/// Failures uniformly return <c>InvalidCredentials</c>; we never distinguish
/// "no such user" from "wrong password" to avoid username enumeration. Inactive
/// accounts and locked-out accounts also return <c>InvalidCredentials</c> from
/// the caller's perspective, with the audit log carrying the real reason.
/// </summary>
public sealed record LocalLoginCommand(string Username, string Password)
    : IRequest<Result<LocalLoginResponse>>, IAuditable;

public sealed class LocalLoginCommandHandler(
    ILocalUserRepository localUserRepository,
    IPasswordHasher passwordHasher,
    ILocalTokenIssuer tokenIssuer,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext,
    TimeProvider timeProvider,
    ILogger<LocalLoginCommandHandler> logger) : IRequestHandler<LocalLoginCommand, Result<LocalLoginResponse>>
{
    private static readonly Error InvalidCredentials = new("InvalidCredentials", "Username or password is incorrect.");

    public async Task<Result<LocalLoginResponse>> Handle(LocalLoginCommand request, CancellationToken cancellationToken)
    {
        // Empty/whitespace inputs short-circuit BEFORE we hit the DB so callers
        // can't probe for tightening behaviour by sending blank strings.
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<LocalLoginResponse>.Failure(InvalidCredentials);
        }

        string normalizedUsername;
        try
        {
            normalizedUsername = LocalUser.NormalizeUsername(request.Username);
        }
        catch (ArgumentException)
        {
            return Result<LocalLoginResponse>.Failure(InvalidCredentials);
        }

        var userResult = await localUserRepository.GetByUsernameAsync(normalizedUsername, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure)
        {
            logger.LogWarning("Local login failed for {Username}: account not found", normalizedUsername);
            return Result<LocalLoginResponse>.Failure(InvalidCredentials);
        }

        var user = userResult.Value!;
        var nowUtc = timeProvider.GetUtcNow();

        if (!user.IsActive)
        {
            logger.LogWarning("Local login rejected for {Username}: account inactive", user.Username);
            return Result<LocalLoginResponse>.Failure(InvalidCredentials);
        }

        if (user.IsLockedOut(nowUtc))
        {
            logger.LogWarning("Local login rejected for {Username}: account locked until {LockoutUntil}", user.Username, user.LockoutUntilUtc);
            return Result<LocalLoginResponse>.Failure(InvalidCredentials);
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash, user.PasswordAlgorithm))
        {
            user.RecordFailedLogin(modifiedBy: $"localuser:{user.Username}");
            await localUserRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
            auditContext.Add(new AuditContextEntry(
                nameof(LocalUser),
                user.Id.ToString(),
                AuditAction.Updated,
                afterPayload: new { reason = "local-login-failed", failedAttemptCount = user.FailedAttemptCount }));
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogWarning("Local login failed for {Username}: bad password (attempt {Attempt})", user.Username, user.FailedAttemptCount);
            return Result<LocalLoginResponse>.Failure(InvalidCredentials);
        }

        user.RecordSuccessfulLogin(nowUtc, modifiedBy: $"localuser:{user.Username}");
        await localUserRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        auditContext.Add(new AuditContextEntry(
            nameof(LocalUser),
            user.Id.ToString(),
            AuditAction.Updated,
            afterPayload: new { reason = "local-login-succeeded", mustChangePassword = user.MustChangePasswordOnNextLogin, role = user.Role.ToString() }));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var (token, expiresAt) = tokenIssuer.IssueAccessToken(user);
        var expiresIn = (long)Math.Max(0, (expiresAt - nowUtc).TotalSeconds);
        logger.LogInformation("Local login succeeded for {Username} (role {Role}, mustChangePassword={MustChange})", user.Username, user.Role, user.MustChangePasswordOnNextLogin);
        return Result<LocalLoginResponse>.Success(new LocalLoginResponse(token, expiresIn, user.MustChangePasswordOnNextLogin));
    }
}
