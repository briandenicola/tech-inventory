using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Owners.Commands;

public sealed record DeleteOwnerCommand(Guid Id) : IRequest<Result>, IAuditable;

// #138 follow-up (superseding D-120, 2026-09-03 product decision — see
// .squad/decisions/inbox/vasquez-owner-deactivation-supersedes-d120.md):
// deactivating an Owner is now allowed while devices still reference it.
// The Owner becomes inactive; existing devices retain their OwnerId as a
// historical reference (FK is unconstrained — see OwnerConfiguration/
// DeviceConfiguration, no cascading delete). Inactive owners are excluded
// from future assignment via the existing IOwnerRepository.ListAsync(
// includeInactive: false) default used by ListOwnersQuery, which the
// admin dropdown already calls. The only remaining guards are not-found
// and already-inactive (still a genuine conflict — you cannot deactivate
// what is already deactivated).
public sealed class DeleteOwnerCommandHandler(
    IOwnerRepository ownerRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<DeleteOwnerCommand, Result>
{
    public async Task<Result> Handle(DeleteOwnerCommand request, CancellationToken cancellationToken)
    {
        var ownerResult = await ownerRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (ownerResult.IsFailure)
        {
            return Result.Failure(ownerResult.Error!);
        }

        var owner = ownerResult.Value!;
        if (!owner.IsActive)
        {
            return Result.Failure(Error.Conflict($"Owner '{request.Id}' is already inactive."));
        }

        var beforeSnapshot = OwnerResponse.FromEntity(owner);
        owner.Deactivate();

        var updateResult = await ownerRepository.UpdateAsync(owner, cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure)
        {
            return Result.Failure(updateResult.Error!);
        }

        auditContext.Set(new AuditContextEntry(nameof(Owner), owner.Id.ToString(), AuditAction.Deleted, beforePayload: beforeSnapshot));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
