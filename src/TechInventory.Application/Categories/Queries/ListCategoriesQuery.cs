using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Categories.Queries;

public sealed record ListCategoriesQuery(int Page = 1, int PageSize = 25, bool IncludeInactive = false) : IRequest<Result<PagedResponse<CategoryResponse>>>;

public sealed class ListCategoriesQueryHandler(ICategoryRepository categoryRepository) : IRequestHandler<ListCategoriesQuery, Result<PagedResponse<CategoryResponse>>>
{
    public async Task<Result<PagedResponse<CategoryResponse>>> Handle(ListCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await categoryRepository.ListAsync(request.IncludeInactive, cancellationToken).ConfigureAwait(false);
        var lookup = categories
            .Where(category => category.ParentId.HasValue)
            .GroupBy(category => category.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray());

        var roots = categories
            .Where(category => category.ParentId is null)
            .OrderBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .Select(BuildResponse)
            .ToArray();

        var pagedRoots = roots
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToArray();

        return Result<PagedResponse<CategoryResponse>>.Success(
            new PagedResponse<CategoryResponse>(pagedRoots, roots.Length, request.Page, request.PageSize));

        CategoryResponse BuildResponse(Category category)
        {
            var children = lookup.TryGetValue(category.Id, out var childCategories)
                ? childCategories.Select(BuildResponse).ToArray()
                : [];
            return CategoryResponse.FromEntity(category, children);
        }
    }
}
