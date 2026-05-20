using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Merges;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Brands.Commands;

public sealed record MergeBrandCommand(Guid SourceId, Guid TargetId)
    : IRequest<Result<MergeReferenceEntityResponse>>, IAuditable, IMergeReferenceEntityCommand;

public sealed class MergeBrandCommandHandler(
    IBrandRepository brandRepository,
    IDeviceRepository deviceRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<MergeBrandCommand, Result<MergeReferenceEntityResponse>>
{
    public async Task<Result<MergeReferenceEntityResponse>> Handle(MergeBrandCommand request, CancellationToken cancellationToken)
    {
        var sourceResult = await brandRepository.GetByIdAsync(request.SourceId, cancellationToken).ConfigureAwait(false);
        if (sourceResult.IsFailure)
        {
            return Result<MergeReferenceEntityResponse>.Failure(sourceResult.Error!);
        }

        var source = sourceResult.Value!;
        if (!source.IsActive)
        {
            return Result<MergeReferenceEntityResponse>.Failure(Error.Conflict($"Brand '{request.SourceId}' is inactive."));
        }

        var targetResult = await brandRepository.GetByIdAsync(request.TargetId, cancellationToken).ConfigureAwait(false);
        if (targetResult.IsFailure)
        {
            return Result<MergeReferenceEntityResponse>.Failure(targetResult.Error!);
        }

        var target = targetResult.Value!;
        if (!target.IsActive)
        {
            return Result<MergeReferenceEntityResponse>.Failure(Error.Conflict($"Brand '{request.TargetId}' is inactive."));
        }

        var sourceBefore = BrandResponse.FromEntity(source);
        var targetBefore = BrandResponse.FromEntity(target);
        var reassignResult = await deviceRepository.ReassignBrandReferencesAsync(source.Id, target.Id, cancellationToken).ConfigureAwait(false);
        if (reassignResult.IsFailure)
        {
            return Result<MergeReferenceEntityResponse>.Failure(reassignResult.Error!);
        }

        var mergedCount = reassignResult.Value!;

        source.Deactivate();
        var updateResult = await brandRepository.UpdateAsync(source, cancellationToken).ConfigureAwait(false);
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

        auditContext.Add(new AuditContextEntry(nameof(Brand), target.Id.ToString(), AuditAction.Updated, beforePayload: targetBefore, afterPayload: auditPayload));
        auditContext.Add(new AuditContextEntry(nameof(Brand), source.Id.ToString(), AuditAction.Deleted, beforePayload: sourceBefore, afterPayload: auditPayload));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<MergeReferenceEntityResponse>.Success(response);
    }
}
