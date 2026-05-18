using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Locations.Queries;

public sealed record ListLocationsQuery(int Page = 1, int PageSize = 25, bool IncludeInactive = false) : IRequest<Result<PagedResponse<LocationResponse>>>;

public sealed class ListLocationsQueryHandler(ILocationRepository locationRepository) : IRequestHandler<ListLocationsQuery, Result<PagedResponse<LocationResponse>>>
{
    public async Task<Result<PagedResponse<LocationResponse>>> Handle(ListLocationsQuery request, CancellationToken cancellationToken)
    {
        var items = await locationRepository.ListAsync(request.IncludeInactive, cancellationToken).ConfigureAwait(false);
        var pagedItems = items
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(LocationResponse.FromEntity)
            .ToArray();

        return Result<PagedResponse<LocationResponse>>.Success(
            new PagedResponse<LocationResponse>(pagedItems, items.Count, request.Page, request.PageSize));
    }
}
