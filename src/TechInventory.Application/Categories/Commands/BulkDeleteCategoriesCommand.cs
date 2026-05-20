using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.BulkOperations;
using TechInventory.Application.Categories;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Categories.Commands;

public sealed record BulkDeleteCategoriesCommand(IReadOnlyList<Guid> CategoryIds)
    : IRequest<Result<BulkOperationResponse>>, IAuditable, IBulkDeleteReferenceEntityCommand
{
    public IReadOnlyList<Guid> Ids => CategoryIds;
}

public sealed class BulkDeleteCategoriesCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<BulkDeleteCategoriesCommand, Result<BulkOperationResponse>>
{
    public async Task<Result<BulkOperationResponse>> Handle(BulkDeleteCategoriesCommand request, CancellationToken cancellationToken)
    {
        var uniqueIds = request.CategoryIds.Distinct().ToArray();
        var allCategories = await categoryRepository.ListAsync(includeInactive: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        var categoriesById = allCategories.ToDictionary(category => category.Id);
        var requestedCategories = new List<Category>(uniqueIds.Length);

        foreach (var categoryId in uniqueIds)
        {
            if (!categoriesById.TryGetValue(categoryId, out var category))
            {
                return Result<BulkOperationResponse>.Failure(Error.NotFound($"Category '{categoryId}' was not found."));
            }

            if (!category.IsActive)
            {
                return Result<BulkOperationResponse>.Failure(Error.Conflict($"Category '{categoryId}' is already inactive."));
            }

            requestedCategories.Add(category);
        }

        var correlationId = Guid.NewGuid();
        foreach (var category in requestedCategories.OrderByDescending(entity => entity.Depth))
        {
            var descendants = GetDescendants(category.Id, allCategories)
                .Where(descendant => descendant.IsActive)
                .ToArray();
            var beforeSnapshot = CategoryResponse.FromEntity(category);

            category.Deactivate();
            var updateResult = await categoryRepository.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
            if (updateResult.IsFailure)
            {
                return Result<BulkOperationResponse>.Failure(updateResult.Error!);
            }

            foreach (var descendant in descendants)
            {
                descendant.Deactivate();
                var descendantUpdateResult = await categoryRepository.UpdateAsync(descendant, cancellationToken).ConfigureAwait(false);
                if (descendantUpdateResult.IsFailure)
                {
                    return Result<BulkOperationResponse>.Failure(descendantUpdateResult.Error!);
                }
            }

            auditContext.Add(new AuditContextEntry(
                nameof(Category),
                category.Id.ToString(),
                AuditAction.Deleted,
                beforePayload: new BulkAuditEnvelope(correlationId, beforeSnapshot),
                afterPayload: new BulkAuditEnvelope(correlationId, CategoryResponse.FromEntity(category))));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<BulkOperationResponse>.Success(new BulkOperationResponse(correlationId, requestedCategories.Count));
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
}
