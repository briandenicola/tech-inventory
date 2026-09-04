using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Abstractions.Services;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.ApiKeys.Commands;

public sealed record RevokeApiKeyCommand(Guid Id) : IRequest<Result>, IAuditable;

public sealed class RevokeApiKeyCommandHandler(
    IApiKeyRepository apiKeyRepository,
    ApiKeyPrincipalResolver principalResolver,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext,
    TimeProvider timeProvider) : IRequestHandler<RevokeApiKeyCommand, Result>
{
    public async Task<Result> Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var principalResult = await principalResolver.ResolveCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (principalResult.IsFailure)
        {
            return Result.Failure(principalResult.Error!);
        }

        var principal = principalResult.Value!;

        var keyResult = await apiKeyRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (keyResult.IsFailure)
        {
            return Result.Failure(keyResult.Error!);
        }

        var apiKey = keyResult.Value!;

        var ownsKey = apiKey.PrincipalType == principal.Type && apiKey.PrincipalId == principal.Id;
        var isAdmin = principal.Role == OwnerRole.Admin;

        // Admins may revoke anyone's key (US-4, emergency revocation); everyone else
        // may only revoke their own. Reported as Forbidden rather than NotFound because
        // the caller supplied a real id — key ids are not secret, so there is nothing
        // to protect by pretending otherwise.
        if (!ownsKey && !isAdmin)
        {
            return Result.Failure(Error.Forbidden("You can only revoke your own API keys."));
        }

        // Idempotent: a second revoke is a no-op that still reports success, so a
        // retried request or a race between the owner and an admin cannot 409.
        if (apiKey.RevokedAt is not null)
        {
            return Result.Success();
        }

        var actor = currentUserService.GetCurrentUserId();
        apiKey.Revoke(actor, timeProvider.GetUtcNow());

        var updateResult = await apiKeyRepository.UpdateAsync(apiKey, cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure)
        {
            return Result.Failure(updateResult.Error!);
        }

        auditContext.Set(new AuditContextEntry(
            nameof(ApiKey),
            apiKey.Id.ToString(),
            AuditAction.Deleted,
            afterPayload: new
            {
                apiKey.Selector,
                apiKey.Name,
                RevokedBy = actor,
                apiKey.RevokedAt,
                RevokedByAdminOnBehalfOfOwner = !ownsKey,
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
