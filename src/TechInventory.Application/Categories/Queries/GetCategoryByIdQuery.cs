using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Categories.Queries;

public sealed record GetCategoryByIdQuery(Guid Id) : IRequest<Result<CategoryResponse>>;

public sealed class GetCategoryByIdQueryHandler(ICategoryRepository categoryRepository) : IRequestHandler<GetCategoryByIdQuery, Result<CategoryResponse>>
{
    public async Task<Result<CategoryResponse>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var categoryResult = await categoryRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (categoryResult.IsFailure)
        {
            return Result<CategoryResponse>.Failure(categoryResult.Error!);
        }

        var categories = await categoryRepository.ListAsync(includeInactive: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        return Result<CategoryResponse>.Success(BuildResponse(categoryResult.Value!, categories));
    }

    private static CategoryResponse BuildResponse(Category category, IReadOnlyList<Category> categories)
    {
        var lookup = categories
            .Where(candidate => candidate.ParentId.HasValue)
            .GroupBy(candidate => candidate.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray());

        return Build(category);

        CategoryResponse Build(Category current)
        {
            var children = lookup.TryGetValue(current.Id, out var childCategories)
                ? childCategories.Select(Build).ToArray()
                : [];
            return CategoryResponse.FromEntity(current, children);
        }
    }
}
