using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Owners.Commands;

public sealed record CreateOwnerCommand(string DisplayName, OwnerRole Role = OwnerRole.Member, Guid? EntraObjectId = null) : IRequest<Result<OwnerResponse>>, IAuditable;

public sealed class CreateOwnerCommandHandler(
    IOwnerRepository ownerRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<CreateOwnerCommand, Result<OwnerResponse>>
{
    public async Task<Result<OwnerResponse>> Handle(CreateOwnerCommand request, CancellationToken cancellationToken)
    {
        var duplicateNameResult = await ownerRepository.GetByNormalizedDisplayNameAsync(request.DisplayName, cancellationToken).ConfigureAwait(false);
        if (duplicateNameResult.IsSuccess)
        {
            return Result<OwnerResponse>.Failure(Error.Conflict($"Owner with display name '{request.DisplayName.Trim()}' already exists."));
        }

        if (request.EntraObjectId.HasValue)
        {
            var duplicateEntraResult = await ownerRepository.GetByEntraObjectIdAsync(request.EntraObjectId.Value, cancellationToken).ConfigureAwait(false);
            if (duplicateEntraResult.IsSuccess)
            {
                return Result<OwnerResponse>.Failure(Error.Conflict($"Owner with EntraObjectId '{request.EntraObjectId.Value}' already exists."));
            }
        }

        var owner = new Owner(Guid.NewGuid(), request.DisplayName, request.Role, request.EntraObjectId);
        var addResult = await ownerRepository.AddAsync(owner, cancellationToken).ConfigureAwait(false);
        if (addResult.IsFailure)
        {
            return Result<OwnerResponse>.Failure(addResult.Error!);
        }

        auditContext.Set(new AuditContextEntry(nameof(Owner), owner.Id.ToString(), AuditAction.Created));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<OwnerResponse>.Success(OwnerResponse.FromEntity(owner));
    }
}
