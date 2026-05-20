using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Merges;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Locations.Commands;

public sealed record MergeLocationCommand(Guid SourceId, Guid TargetId)
    : IRequest<Result<MergeReferenceEntityResponse>>, IAuditable, IMergeReferenceEntityCommand;

public sealed class MergeLocationCommandHandler(
    ILocationRepository locationRepository,
    IDeviceRepository deviceRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<MergeLocationCommand, Result<MergeReferenceEntityResponse>>
{
    public async Task<Result<MergeReferenceEntityResponse>> Handle(MergeLocationCommand request, CancellationToken cancellationToken)
    {
        var sourceResult = await locationRepository.GetByIdAsync(request.SourceId, cancellationToken).ConfigureAwait(false);
        if (sourceResult.IsFailure)
        {
            return Result<MergeReferenceEntityResponse>.Failure(sourceResult.Error!);
        }

        var source = sourceResult.Value!;
        if (!source.IsActive)
        {
            return Result<MergeReferenceEntityResponse>.Failure(Error.Conflict($"Location '{request.SourceId}' is inactive."));
        }

        var targetResult = await locationRepository.GetByIdAsync(request.TargetId, cancellationToken).ConfigureAwait(false);
        if (targetResult.IsFailure)
        {
            return Result<MergeReferenceEntityResponse>.Failure(targetResult.Error!);
        }

        var target = targetResult.Value!;
        if (!target.IsActive)
        {
            return Result<MergeReferenceEntityResponse>.Failure(Error.Conflict($"Location '{request.TargetId}' is inactive."));
        }

        var sourceBefore = LocationResponse.FromEntity(source);
        var targetBefore = LocationResponse.FromEntity(target);
        var reassignResult = await deviceRepository.ReassignLocationReferencesAsync(source.Id, target.Id, cancellationToken).ConfigureAwait(false);
        if (reassignResult.IsFailure)
        {
            return Result<MergeReferenceEntityResponse>.Failure(reassignResult.Error!);
        }

        var mergedCount = reassignResult.Value!;

        source.Deactivate();
        var updateResult = await locationRepository.UpdateAsync(source, cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure)
        {
            return Result<MergeReferenceEntityResponse>.Failure(updateResult.Error!);
        }

        var response = new MergeReferenceEntityResponse(mergedCount, source.Id, target.Id);
        var auditPayload = new
        {
            response.SourceId,
            response.TargetId,
            response.MergedCount,
            SourceName = sourceBefore.Name,
            TargetName = targetBefore.Name,
            SourceDeactivated = true,
        };

        auditContext.Add(new AuditContextEntry(nameof(Location), target.Id.ToString(), AuditAction.Updated, beforePayload: targetBefore, afterPayload: auditPayload));
        auditContext.Add(new AuditContextEntry(nameof(Location), source.Id.ToString(), AuditAction.Deleted, beforePayload: sourceBefore, afterPayload: auditPayload));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<MergeReferenceEntityResponse>.Success(response);
    }
}
