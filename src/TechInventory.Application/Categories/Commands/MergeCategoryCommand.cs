using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Merges;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Categories.Commands;

public sealed record MergeCategoryCommand(Guid SourceId, Guid TargetId)
    : IRequest<Result<MergeReferenceEntityResponse>>, IAuditable, IMergeReferenceEntityCommand;

public sealed class MergeCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IDeviceRepository deviceRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<MergeCategoryCommand, Result<MergeReferenceEntityResponse>>
{
    public async Task<Result<MergeReferenceEntityResponse>> Handle(MergeCategoryCommand request, CancellationToken cancellationToken)
    {
        var sourceResult = await categoryRepository.GetByIdAsync(request.SourceId, cancellationToken).ConfigureAwait(false);
        if (sourceResult.IsFailure)
        {
            return Result<MergeReferenceEntityResponse>.Failure(sourceResult.Error!);
        }

        var source = sourceResult.Value!;
        if (!source.IsActive)
        {
            return Result<MergeReferenceEntityResponse>.Failure(Error.Conflict($"Category '{request.SourceId}' is inactive."));
        }

        var targetResult = await categoryRepository.GetByIdAsync(request.TargetId, cancellationToken).ConfigureAwait(false);
        if (targetResult.IsFailure)
        {
            return Result<MergeReferenceEntityResponse>.Failure(targetResult.Error!);
        }

        var target = targetResult.Value!;
        if (!target.IsActive)
        {
            return Result<MergeReferenceEntityResponse>.Failure(Error.Conflict($"Category '{request.TargetId}' is inactive."));
        }

        var allCategories = await categoryRepository.ListAsync(includeInactive: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (IsDescendant(target.Id, source.Id, allCategories))
        {
            return Result<MergeReferenceEntityResponse>.Failure(Error.Conflict("A category cannot be merged into one of its descendants."));
        }

        var descendants = GetDescendants(source.Id, allCategories).ToArray();
        var depthDelta = target.Depth - source.Depth;
        if (descendants.Any(descendant => descendant.Depth + depthDelta > 3))
        {
            return Result<MergeReferenceEntityResponse>.Failure(Error.Conflict("Merging these categories would push a descendant beyond depth 3."));
        }

        var sourceBefore = CategoryResponse.FromEntity(source);
        var targetBefore = CategoryResponse.FromEntity(target);
        var reassignResult = await deviceRepository.ReassignCategoryReferencesAsync(source.Id, target.Id, cancellationToken).ConfigureAwait(false);
        if (reassignResult.IsFailure)
        {
            return Result<MergeReferenceEntityResponse>.Failure(reassignResult.Error!);
        }

        var mergedCount = reassignResult.Value!;

        foreach (var descendant in descendants.OrderBy(category => category.Depth))
        {
            var nextParentId = descendant.ParentId == source.Id ? target.Id : descendant.ParentId;
            descendant.Reparent(nextParentId, descendant.Depth + depthDelta);
            var descendantUpdateResult = await categoryRepository.UpdateAsync(descendant, cancellationToken).ConfigureAwait(false);
            if (descendantUpdateResult.IsFailure)
            {
                return Result<MergeReferenceEntityResponse>.Failure(descendantUpdateResult.Error!);
            }
        }

        source.Deactivate();
        var sourceUpdateResult = await categoryRepository.UpdateAsync(source, cancellationToken).ConfigureAwait(false);
        if (sourceUpdateResult.IsFailure)
        {
            return Result<MergeReferenceEntityResponse>.Failure(sourceUpdateResult.Error!);
        }

        var response = new MergeReferenceEntityResponse(mergedCount, source.Id, target.Id);
        var auditPayload = new
        {
            response.SourceId,
            response.TargetId,
            response.MergedCount,
            SourceName = sourceBefore.Name,
            TargetName = targetBefore.Name,
            ReparentedCategoryCount = descendants.Length,
            SourceDeactivated = true,
        };

        auditContext.Add(new AuditContextEntry(nameof(Category), target.Id.ToString(), AuditAction.Updated, beforePayload: targetBefore, afterPayload: auditPayload));
        auditContext.Add(new AuditContextEntry(nameof(Category), source.Id.ToString(), AuditAction.Deleted, beforePayload: sourceBefore, afterPayload: auditPayload));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<MergeReferenceEntityResponse>.Success(response);
    }

    private static IEnumerable<Category> GetDescendants(Guid categoryId, IEnumerable<Category> categories)
    {
        var lookup = categories
            .Where(candidate => candidate.ParentId.HasValue)
            .GroupBy(candidate => candidate.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var stack = new Stack<Guid>();
        stack.Push(categoryId);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!lookup.TryGetValue(current, out var children))
            {
                continue;
            }

            foreach (var child in children)
            {
                yield return child;
                stack.Push(child.Id);
            }
        }
    }

    private static bool IsDescendant(Guid candidateTargetId, Guid sourceId, IEnumerable<Category> categories)
        => GetDescendants(sourceId, categories).Any(descendant => descendant.Id == candidateTargetId);
}
