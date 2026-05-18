using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Brands.Queries;

public sealed record ListBrandsQuery(int Page = 1, int PageSize = 25, bool IncludeInactive = false) : IRequest<Result<PagedResponse<BrandResponse>>>;

public sealed class ListBrandsQueryHandler(IBrandRepository brandRepository) : IRequestHandler<ListBrandsQuery, Result<PagedResponse<BrandResponse>>>
{
    public async Task<Result<PagedResponse<BrandResponse>>> Handle(ListBrandsQuery request, CancellationToken cancellationToken)
    {
        var items = await brandRepository.ListAsync(request.IncludeInactive, cancellationToken).ConfigureAwait(false);
        var pagedItems = items
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(BrandResponse.FromEntity)
            .ToArray();

        return Result<PagedResponse<BrandResponse>>.Success(
            new PagedResponse<BrandResponse>(pagedItems, items.Count, request.Page, request.PageSize));
    }
}
