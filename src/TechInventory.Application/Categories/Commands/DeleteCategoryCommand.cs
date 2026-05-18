using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Categories.Commands;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest<Result>, IAuditable;

public sealed class DeleteCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<DeleteCategoryCommand, Result>
{
    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var categoryResult = await categoryRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (categoryResult.IsFailure)
        {
            return Result.Failure(categoryResult.Error!);
        }

        var category = categoryResult.Value!;
        if (!category.IsActive)
        {
            return Result.Failure(Error.Conflict($"Category '{request.Id}' is already inactive."));
        }

        var allCategories = await categoryRepository.ListAsync(includeInactive: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        var descendants = GetDescendants(category.Id, allCategories).Where(descendant => descendant.IsActive).ToArray();
        var beforeSnapshot = CategoryResponse.FromEntity(category);

        category.Deactivate();
        var updateResult = await categoryRepository.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure)
        {
            return Result.Failure(updateResult.Error!);
        }

        foreach (var descendant in descendants)
        {
            descendant.Deactivate();
            var descendantUpdateResult = await categoryRepository.UpdateAsync(descendant, cancellationToken).ConfigureAwait(false);
            if (descendantUpdateResult.IsFailure)
            {
                return Result.Failure(descendantUpdateResult.Error!);
            }
        }

        auditContext.Set(new AuditContextEntry(nameof(Category), category.Id.ToString(), AuditAction.Deleted, beforePayload: beforeSnapshot));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
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
