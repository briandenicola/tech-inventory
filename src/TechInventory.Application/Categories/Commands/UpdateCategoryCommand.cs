using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Categories.Commands;

public sealed record UpdateCategoryCommand(Guid Id, string Name, Guid? ParentId = null, string? Icon = null) : IRequest<Result<CategoryResponse>>, IAuditable;

public sealed class UpdateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<UpdateCategoryCommand, Result<CategoryResponse>>
{
    public async Task<Result<CategoryResponse>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var categoryResult = await categoryRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (categoryResult.IsFailure)
        {
            return Result<CategoryResponse>.Failure(categoryResult.Error!);
        }

        var category = categoryResult.Value!;
        var allCategories = await categoryRepository.ListAsync(includeInactive: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (request.ParentId == category.Id)
        {
            return Result<CategoryResponse>.Failure(Error.Conflict("A category cannot be its own parent."));
        }

        if (request.ParentId.HasValue && IsDescendant(request.ParentId.Value, category.Id, allCategories))
        {
            return Result<CategoryResponse>.Failure(Error.Conflict("A category cannot be moved beneath one of its descendants."));
        }

        var duplicateResult = await categoryRepository.GetByNameWithinParentAsync(request.Name, request.ParentId, cancellationToken).ConfigureAwait(false);
        if (duplicateResult.IsSuccess && duplicateResult.Value!.Id != category.Id)
        {
            return Result<CategoryResponse>.Failure(Error.Conflict($"Category with name '{request.Name.Trim()}' already exists under the selected parent."));
        }

        var targetDepthResult = await ResolveDepthAsync(request.ParentId, cancellationToken).ConfigureAwait(false);
        if (targetDepthResult.IsFailure)
        {
            return Result<CategoryResponse>.Failure(targetDepthResult.Error!);
        }

        var depthDelta = targetDepthResult.Value! - category.Depth;
        var descendants = GetDescendants(category.Id, allCategories).ToArray();
        if (descendants.Any(descendant => descendant.Depth + depthDelta > 3))
        {
            return Result<CategoryResponse>.Failure(Error.Conflict("Updating this category would push a descendant beyond depth 3."));
        }

        var beforeSnapshot = CategoryResponse.FromEntity(category);
        category.Rename(request.Name);
        category.UpdateIcon(request.Icon);
        category.Reparent(request.ParentId, targetDepthResult.Value!);
        if (!category.IsActive)
        {
            category.Reactivate();
        }

        var updateResult = await categoryRepository.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure)
        {
            return Result<CategoryResponse>.Failure(updateResult.Error!);
        }

        if (depthDelta != 0)
        {
            foreach (var descendant in descendants)
            {
                descendant.Reparent(descendant.ParentId, descendant.Depth + depthDelta);
                var descendantUpdateResult = await categoryRepository.UpdateAsync(descendant, cancellationToken).ConfigureAwait(false);
                if (descendantUpdateResult.IsFailure)
                {
                    return Result<CategoryResponse>.Failure(descendantUpdateResult.Error!);
                }
            }
        }

        auditContext.Set(new AuditContextEntry(nameof(Category), category.Id.ToString(), AuditAction.Updated, beforePayload: beforeSnapshot));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<CategoryResponse>.Success(CategoryResponse.FromEntity(category));
    }

    private async Task<Result<int>> ResolveDepthAsync(Guid? parentId, CancellationToken cancellationToken)
    {
        if (!parentId.HasValue)
        {
            return Result<int>.Success(1);
        }

        var parentResult = await categoryRepository.GetByIdAsync(parentId.Value, cancellationToken).ConfigureAwait(false);
        if (parentResult.IsFailure)
        {
            return Result<int>.Failure(parentResult.Error!);
        }

        if (!parentResult.Value!.IsActive)
        {
            return Result<int>.Failure(Error.Conflict($"Parent category '{parentId.Value}' is inactive."));
        }

        if (parentResult.Value.Depth >= 3)
        {
            return Result<int>.Failure(Error.Conflict("Categories cannot exceed a depth of 3."));
        }

        return Result<int>.Success(parentResult.Value.Depth + 1);
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

    private static bool IsDescendant(Guid candidateParentId, Guid categoryId, IEnumerable<Category> categories)
        => GetDescendants(categoryId, categories).Any(descendant => descendant.Id == candidateParentId);
}
