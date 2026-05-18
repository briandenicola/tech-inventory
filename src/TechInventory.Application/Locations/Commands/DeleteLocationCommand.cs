using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Locations.Commands;

public sealed record DeleteLocationCommand(Guid Id) : IRequest<Result>, IAuditable;

public sealed class DeleteLocationCommandHandler(
    ILocationRepository locationRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<DeleteLocationCommand, Result>
{
    public async Task<Result> Handle(DeleteLocationCommand request, CancellationToken cancellationToken)
    {
        var entityResult = await locationRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entityResult.IsFailure)
        {
            return Result.Failure(entityResult.Error!);
        }

        var entity = entityResult.Value!;
        if (!entity.IsActive)
        {
            return Result.Failure(Error.Conflict($"Location '{request.Id}' is already inactive."));
        }

        var beforeSnapshot = LocationResponse.FromEntity(entity);
        entity.Deactivate();

        var updateResult = await locationRepository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure)
        {
            return Result.Failure(updateResult.Error!);
        }

        auditContext.Set(new AuditContextEntry(nameof(Location), entity.Id.ToString(), AuditAction.Deleted, beforePayload: beforeSnapshot));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
