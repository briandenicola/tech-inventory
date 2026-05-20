using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.BulkOperations;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Locations;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Locations.Commands;

public sealed record BulkDeleteLocationsCommand(IReadOnlyList<Guid> LocationIds)
    : IRequest<Result<BulkOperationResponse>>, IAuditable, IBulkDeleteReferenceEntityCommand
{
    public IReadOnlyList<Guid> Ids => LocationIds;
}

public sealed class BulkDeleteLocationsCommandHandler(
    ILocationRepository locationRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<BulkDeleteLocationsCommand, Result<BulkOperationResponse>>
{
    public async Task<Result<BulkOperationResponse>> Handle(BulkDeleteLocationsCommand request, CancellationToken cancellationToken)
    {
        var uniqueIds = request.LocationIds.Distinct().ToArray();
        var locations = new List<Location>(uniqueIds.Length);

        foreach (var locationId in uniqueIds)
        {
            var locationResult = await locationRepository.GetByIdAsync(locationId, cancellationToken).ConfigureAwait(false);
            if (locationResult.IsFailure)
            {
                return Result<BulkOperationResponse>.Failure(locationResult.Error!);
            }

            var location = locationResult.Value!;
            if (!location.IsActive)
            {
                return Result<BulkOperationResponse>.Failure(Error.Conflict($"Location '{locationId}' is already inactive."));
            }

            locations.Add(location);
        }

        var correlationId = Guid.NewGuid();
        foreach (var location in locations)
        {
            var beforeSnapshot = LocationResponse.FromEntity(location);
            location.Deactivate();

            var updateResult = await locationRepository.UpdateAsync(location, cancellationToken).ConfigureAwait(false);
            if (updateResult.IsFailure)
            {
                return Result<BulkOperationResponse>.Failure(updateResult.Error!);
            }

            auditContext.Add(new AuditContextEntry(
                nameof(Location),
                location.Id.ToString(),
                AuditAction.Deleted,
                beforePayload: new BulkAuditEnvelope(correlationId, beforeSnapshot),
                afterPayload: new BulkAuditEnvelope(correlationId, LocationResponse.FromEntity(location))));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<BulkOperationResponse>.Success(new BulkOperationResponse(correlationId, locations.Count));
    }
}
