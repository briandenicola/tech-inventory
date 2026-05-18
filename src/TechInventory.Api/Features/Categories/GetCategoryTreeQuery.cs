using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Categories;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Api.Features.Categories;

public sealed record GetCategoryTreeQuery(bool IncludeInactive = false) : IRequest<Result<IReadOnlyList<CategoryResponse>>>;

public sealed class GetCategoryTreeQueryHandler(ICategoryRepository categoryRepository) : IRequestHandler<GetCategoryTreeQuery, Result<IReadOnlyList<CategoryResponse>>>
{
    public async Task<Result<IReadOnlyList<CategoryResponse>>> Handle(GetCategoryTreeQuery request, CancellationToken cancellationToken)
    {
        var categories = await categoryRepository.ListAsync(request.IncludeInactive, cancellationToken);
        var lookup = categories
            .Where(category => category.ParentId.HasValue)
            .GroupBy(category => category.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray());

        var roots = categories
            .Where(category => category.ParentId is null)
            .OrderBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .Select(BuildResponse)
            .ToArray();

        return Result<IReadOnlyList<CategoryResponse>>.Success(roots);

        CategoryResponse BuildResponse(Category category)
        {
            var children = lookup.TryGetValue(category.Id, out var childCategories)
                ? childCategories.Select(BuildResponse).ToArray()
                : [];

            return CategoryResponse.FromEntity(category, children);
        }
    }
}
