using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Owners.Queries;

public sealed record ListOwnersQuery(int Page = 1, int PageSize = 25, bool IncludeInactive = false) : IRequest<Result<PagedResponse<OwnerResponse>>>;

public sealed class ListOwnersQueryHandler(IOwnerRepository ownerRepository) : IRequestHandler<ListOwnersQuery, Result<PagedResponse<OwnerResponse>>>
{
    public async Task<Result<PagedResponse<OwnerResponse>>> Handle(ListOwnersQuery request, CancellationToken cancellationToken)
    {
        var items = await ownerRepository.ListAsync(request.IncludeInactive, cancellationToken).ConfigureAwait(false);
        var pagedItems = items
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(OwnerResponse.FromEntity)
            .ToArray();

        return Result<PagedResponse<OwnerResponse>>.Success(
            new PagedResponse<OwnerResponse>(pagedItems, items.Count, request.Page, request.PageSize));
    }
}
