using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.ApiKeys.Queries;

/// <summary>Lists the calling principal's own keys. Never returns key material.</summary>
public sealed record ListApiKeysQuery(int Page = 1, int PageSize = 25)
    : IRequest<Result<PagedResponse<ApiKeyResponse>>>;

public sealed class ListApiKeysQueryHandler(
    IApiKeyRepository apiKeyRepository,
    ApiKeyPrincipalResolver principalResolver,
    TimeProvider timeProvider) : IRequestHandler<ListApiKeysQuery, Result<PagedResponse<ApiKeyResponse>>>
{
    public async Task<Result<PagedResponse<ApiKeyResponse>>> Handle(ListApiKeysQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var principalResult = await principalResolver.ResolveCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (principalResult.IsFailure)
        {
            return Result<PagedResponse<ApiKeyResponse>>.Failure(principalResult.Error!);
        }

        var principal = principalResult.Value!;
        var now = timeProvider.GetUtcNow();

        var page = await apiKeyRepository
            .GetByPrincipalAsync(principal.Type, principal.Id, new PageRequest(request.Page, request.PageSize), cancellationToken)
            .ConfigureAwait(false);

        var items = page.Items.Select(key => ApiKeyResponse.FromEntity(key, now)).ToArray();

        return Result<PagedResponse<ApiKeyResponse>>.Success(
            new PagedResponse<ApiKeyResponse>(items, page.TotalCount, page.Page, page.PageSize));
    }
}
