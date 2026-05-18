using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Locations.Commands;

public sealed record UpdateLocationCommand(Guid Id, string Name, TechInventory.Domain.Enums.LocationType Type) : IRequest<Result<LocationResponse>>, IAuditable;

public sealed class UpdateLocationCommandHandler(
    ILocationRepository locationRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<UpdateLocationCommand, Result<LocationResponse>>
{
    public async Task<Result<LocationResponse>> Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
    {
        var entityResult = await locationRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entityResult.IsFailure)
        {
            return Result<LocationResponse>.Failure(entityResult.Error!);
        }

        var entity = entityResult.Value!;
        var duplicateResult = await locationRepository.GetByNormalizedNameAsync(request.Name, cancellationToken).ConfigureAwait(false);
        if (duplicateResult.IsSuccess && duplicateResult.Value!.Id != entity.Id)
        {
            return Result<LocationResponse>.Failure(Error.Conflict($"Location with name '{request.Name.Trim()}' already exists."));
        }

        var beforeSnapshot = LocationResponse.FromEntity(entity);

        try
        {
            entity.Rename(request.Name);
            entity.SetType(request.Type);
            if (!entity.IsActive)
            {
                entity.Reactivate();
            }
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            return Result<LocationResponse>.Failure(Error.Conflict(exception.Message));
        }

        var updateResult = await locationRepository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure)
        {
            return Result<LocationResponse>.Failure(updateResult.Error!);
        }

        auditContext.Set(new AuditContextEntry(nameof(Location), entity.Id.ToString(), AuditAction.Updated, beforePayload: beforeSnapshot));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<LocationResponse>.Success(LocationResponse.FromEntity(entity));
    }
}
