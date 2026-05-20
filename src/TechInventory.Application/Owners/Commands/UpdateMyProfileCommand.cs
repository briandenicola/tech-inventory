using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Owners.Commands;

/// <summary>
/// F020 v1 — self-service display-name update.
///
/// Looks up the current user's Owner row by their Entra object id (so the
/// controller can stay role-agnostic — any authenticated user may rename
/// themselves) and updates the display name only. Role and Entra link are
/// intentionally NOT mutated here; Admins still manage those via
/// <see cref="UpdateOwnerCommand"/>.
///
/// Emits an <c>Owner Updated</c> audit row via the standard pipeline so the
/// change shows up in <c>/admin/audit?entityType=Owner&amp;entityId=...</c>.
/// </summary>
public sealed record UpdateMyProfileCommand(Guid EntraObjectId, string DisplayName)
    : IRequest<Result<OwnerResponse>>, IAuditable;

public sealed class UpdateMyProfileCommandHandler(
    IOwnerRepository ownerRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<UpdateMyProfileCommand, Result<OwnerResponse>>
{
    public async Task<Result<OwnerResponse>> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var ownerResult = await ownerRepository.GetByEntraObjectIdAsync(request.EntraObjectId, cancellationToken).ConfigureAwait(false);
        if (ownerResult.IsFailure)
        {
            return Result<OwnerResponse>.Failure(ownerResult.Error!);
        }

        var owner = ownerResult.Value!;

        var trimmed = request.DisplayName?.Trim() ?? string.Empty;
        if (string.Equals(owner.DisplayName, trimmed, StringComparison.Ordinal))
        {
            return Result<OwnerResponse>.Success(OwnerResponse.FromEntity(owner));
        }

        var duplicateNameResult = await ownerRepository.GetByNormalizedDisplayNameAsync(trimmed, cancellationToken).ConfigureAwait(false);
        if (duplicateNameResult.IsSuccess && duplicateNameResult.Value!.Id != owner.Id)
        {
            return Result<OwnerResponse>.Failure(Error.Conflict($"Display name '{trimmed}' is already in use."));
        }

        var beforeSnapshot = OwnerResponse.FromEntity(owner);
        owner.Rename(trimmed);

        var updateResult = await ownerRepository.UpdateAsync(owner, cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure)
        {
            return Result<OwnerResponse>.Failure(updateResult.Error!);
        }

        auditContext.Set(new AuditContextEntry(nameof(Owner), owner.Id.ToString(), AuditAction.Updated, beforePayload: beforeSnapshot));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<OwnerResponse>.Success(OwnerResponse.FromEntity(owner));
    }
}
