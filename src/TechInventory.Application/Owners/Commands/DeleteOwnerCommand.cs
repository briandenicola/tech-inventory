using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Owners.Commands;

public sealed record DeleteOwnerCommand(Guid Id) : IRequest<Result>, IAuditable;

public sealed class DeleteOwnerCommandHandler(
    IOwnerRepository ownerRepository,
    IDeviceRepository deviceRepository,
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

        if (await HasAssignedDevicesAsync(request.Id, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(Error.Conflict($"Owner '{request.Id}' cannot be deleted while devices still reference it."));
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

    private async Task<bool> HasAssignedDevicesAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        foreach (var status in Enum.GetValues<DeviceStatus>())
        {
            var page = await deviceRepository.ListAsync(
                new DeviceListCriteria(new PageRequest(1, 1), ownerId: ownerId, status: status),
                cancellationToken).ConfigureAwait(false);

            if (page.TotalCount > 0)
            {
                return true;
            }
        }

        return false;
    }
}
