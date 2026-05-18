using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Tags.Queries;

public sealed record ListTagsQuery(int Page = 1, int PageSize = 25, bool IncludeInactive = false) : IRequest<Result<PagedResponse<TagResponse>>>;

public sealed class ListTagsQueryHandler(ITagRepository tagRepository) : IRequestHandler<ListTagsQuery, Result<PagedResponse<TagResponse>>>
{
    public async Task<Result<PagedResponse<TagResponse>>> Handle(ListTagsQuery request, CancellationToken cancellationToken)
    {
        var items = await tagRepository.ListAsync(request.IncludeInactive, cancellationToken).ConfigureAwait(false);
        var pagedItems = items
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(TagResponse.FromEntity)
            .ToArray();

        return Result<PagedResponse<TagResponse>>.Success(
            new PagedResponse<TagResponse>(pagedItems, items.Count, request.Page, request.PageSize));
    }
}
