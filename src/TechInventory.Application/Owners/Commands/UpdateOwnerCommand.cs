using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Owners.Commands;

public sealed record UpdateOwnerCommand(Guid Id, string DisplayName, OwnerRole Role, Guid? EntraObjectId = null) : IRequest<Result<OwnerResponse>>, IAuditable;

public sealed class UpdateOwnerCommandHandler(
    IOwnerRepository ownerRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<UpdateOwnerCommand, Result<OwnerResponse>>
{
    public async Task<Result<OwnerResponse>> Handle(UpdateOwnerCommand request, CancellationToken cancellationToken)
    {
        var ownerResult = await ownerRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (ownerResult.IsFailure)
        {
            return Result<OwnerResponse>.Failure(ownerResult.Error!);
        }

        var owner = ownerResult.Value!;
        var duplicateNameResult = await ownerRepository.GetByNormalizedDisplayNameAsync(request.DisplayName, cancellationToken).ConfigureAwait(false);
        if (duplicateNameResult.IsSuccess && duplicateNameResult.Value!.Id != owner.Id)
        {
            return Result<OwnerResponse>.Failure(Error.Conflict($"Owner with display name '{request.DisplayName.Trim()}' already exists."));
        }

        if (request.EntraObjectId.HasValue)
        {
            var duplicateEntraResult = await ownerRepository.GetByEntraObjectIdAsync(request.EntraObjectId.Value, cancellationToken).ConfigureAwait(false);
            if (duplicateEntraResult.IsSuccess && duplicateEntraResult.Value!.Id != owner.Id)
            {
                return Result<OwnerResponse>.Failure(Error.Conflict($"Owner with EntraObjectId '{request.EntraObjectId.Value}' already exists."));
            }
        }

        var beforeSnapshot = OwnerResponse.FromEntity(owner);
        owner.Rename(request.DisplayName);
        owner.SetRole(request.Role);
        owner.LinkEntraIdentity(request.EntraObjectId);
        if (!owner.IsActive)
        {
            owner.Reactivate();
        }

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
