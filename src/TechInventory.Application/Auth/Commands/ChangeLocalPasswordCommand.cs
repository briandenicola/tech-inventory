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
/// F025 — change the password for the currently authenticated local user.
///
/// Verifies the current password (no shortcut, even when
/// <c>MustChangePasswordOnNextLogin = true</c>) and stores a fresh hash with
/// the current algorithm tag. On success the must-change flag is cleared so the
/// account can use other endpoints.
///
/// The acting user is taken from <see cref="ICurrentUserService"/> — the
/// controller passes the authenticated subject id along; we resolve the
/// <see cref="LocalUser"/> from the repository to avoid trusting the request
/// body for identity.
/// </summary>
public sealed record ChangeLocalPasswordCommand(
    Guid LocalUserId,
    string CurrentPassword,
    string NewPassword) : IRequest<Result>, IAuditable;

public sealed class ChangeLocalPasswordCommandHandler(
    ILocalUserRepository localUserRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext,
    ILogger<ChangeLocalPasswordCommandHandler> logger) : IRequestHandler<ChangeLocalPasswordCommand, Result>
{
    private static readonly Error InvalidCurrentPassword = new("InvalidCredentials", "Current password is incorrect.");
    private static readonly Error PasswordSameAsCurrent = new("Validation", "New password must differ from the current password.");

    public async Task<Result> Handle(ChangeLocalPasswordCommand request, CancellationToken cancellationToken)
    {
        var userResult = await localUserRepository.GetByIdAsync(request.LocalUserId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure)
        {
            return Result.Failure(userResult.Error!);
        }

        var user = userResult.Value!;
        if (!user.IsActive)
        {
            return Result.Failure(InvalidCurrentPassword);
        }

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash, user.PasswordAlgorithm))
        {
            logger.LogWarning("Local change-password failed for {Username}: wrong current password", user.Username);
            return Result.Failure(InvalidCurrentPassword);
        }

        if (passwordHasher.Verify(request.NewPassword, user.PasswordHash, user.PasswordAlgorithm))
        {
            return Result.Failure(PasswordSameAsCurrent);
        }

        var newHash = passwordHasher.Hash(request.NewPassword);
        user.SetPassword(newHash, passwordHasher.CurrentAlgorithm, requireChangeOnNextLogin: false, modifiedBy: $"localuser:{user.Username}");

        var updateResult = await localUserRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure)
        {
            return Result.Failure(updateResult.Error!);
        }

        auditContext.Set(new AuditContextEntry(
            nameof(LocalUser),
            user.Id.ToString(),
            AuditAction.Updated,
            afterPayload: new { reason = "local-password-changed" }));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Local password changed for {Username}", user.Username);
        return Result.Success();
    }
}
