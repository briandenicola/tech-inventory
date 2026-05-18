using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Networks.Queries;

public sealed record ListNetworksQuery(int Page = 1, int PageSize = 25, bool IncludeInactive = false) : IRequest<Result<PagedResponse<NetworkResponse>>>;

public sealed class ListNetworksQueryHandler(INetworkRepository networkRepository) : IRequestHandler<ListNetworksQuery, Result<PagedResponse<NetworkResponse>>>
{
    public async Task<Result<PagedResponse<NetworkResponse>>> Handle(ListNetworksQuery request, CancellationToken cancellationToken)
    {
        var items = await networkRepository.ListAsync(request.IncludeInactive, cancellationToken).ConfigureAwait(false);
        var pagedItems = items
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(NetworkResponse.FromEntity)
            .ToArray();

        return Result<PagedResponse<NetworkResponse>>.Success(
            new PagedResponse<NetworkResponse>(pagedItems, items.Count, request.Page, request.PageSize));
    }
}
