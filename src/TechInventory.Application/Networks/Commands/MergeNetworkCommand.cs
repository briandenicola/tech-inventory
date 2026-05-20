using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Merges;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Networks.Commands;

public sealed record MergeNetworkCommand(Guid SourceId, Guid TargetId)
    : IRequest<Result<MergeReferenceEntityResponse>>, IAuditable, IMergeReferenceEntityCommand;

public sealed class MergeNetworkCommandHandler(
    INetworkRepository networkRepository,
    IDeviceRepository deviceRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<MergeNetworkCommand, Result<MergeReferenceEntityResponse>>
{
    public async Task<Result<MergeReferenceEntityResponse>> Handle(MergeNetworkCommand request, CancellationToken cancellationToken)
    {
        var sourceResult = await networkRepository.GetByIdAsync(request.SourceId, cancellationToken).ConfigureAwait(false);
        if (sourceResult.IsFailure)
        {
            return Result<MergeReferenceEntityResponse>.Failure(sourceResult.Error!);
        }

        var source = sourceResult.Value!;
        if (!source.IsActive)
        {
            return Result<MergeReferenceEntityResponse>.Failure(Error.Conflict($"Network '{request.SourceId}' is inactive."));
        }

        var targetResult = await networkRepository.GetByIdAsync(request.TargetId, cancellationToken).ConfigureAwait(false);
        if (targetResult.IsFailure)
        {
            return Result<MergeReferenceEntityResponse>.Failure(targetResult.Error!);
        }

        var target = targetResult.Value!;
        if (!target.IsActive)
        {
            return Result<MergeReferenceEntityResponse>.Failure(Error.Conflict($"Network '{request.TargetId}' is inactive."));
        }

        var sourceBefore = NetworkResponse.FromEntity(source);
        var targetBefore = NetworkResponse.FromEntity(target);
        var reassignResult = await deviceRepository.ReassignNetworkReferencesAsync(source.Id, target.Id, cancellationToken).ConfigureAwait(false);
        if (reassignResult.IsFailure)
        {
            return Result<MergeReferenceEntityResponse>.Failure(reassignResult.Error!);
        }

        var mergedCount = reassignResult.Value!;

        source.Deactivate();
        var updateResult = await networkRepository.UpdateAsync(source, cancellationToken).ConfigureAwait(false);
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

        auditContext.Add(new AuditContextEntry(nameof(Network), target.Id.ToString(), AuditAction.Updated, beforePayload: targetBefore, afterPayload: auditPayload));
        auditContext.Add(new AuditContextEntry(nameof(Network), source.Id.ToString(), AuditAction.Deleted, beforePayload: sourceBefore, afterPayload: auditPayload));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<MergeReferenceEntityResponse>.Success(response);
    }
}
