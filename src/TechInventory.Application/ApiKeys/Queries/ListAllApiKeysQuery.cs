using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.ApiKeys.Queries;

/// <summary>
/// Admin view of every key in the household, for emergency revocation (US-4).
/// Authorization is enforced at the controller by the Admin policy.
/// </summary>
public sealed record ListAllApiKeysQuery(int Page = 1, int PageSize = 25)
    : IRequest<Result<PagedResponse<ApiKeyResponse>>>;

public sealed class ListAllApiKeysQueryHandler(
    IApiKeyRepository apiKeyRepository,
    IOwnerRepository ownerRepository,
    ILocalUserRepository localUserRepository,
    TimeProvider timeProvider) : IRequestHandler<ListAllApiKeysQuery, Result<PagedResponse<ApiKeyResponse>>>
{
    public async Task<Result<PagedResponse<ApiKeyResponse>>> Handle(ListAllApiKeysQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = timeProvider.GetUtcNow();
        var page = await apiKeyRepository
            .ListAllAsync(new PageRequest(request.Page, request.PageSize), cancellationToken)
            .ConfigureAwait(false);

        var items = new List<ApiKeyResponse>(page.Items.Count);
        foreach (var key in page.Items)
        {
            items.Add(ApiKeyResponse.FromEntity(key, now, await ResolveDisplayNameAsync(key, cancellationToken).ConfigureAwait(false)));
        }

        return Result<PagedResponse<ApiKeyResponse>>.Success(
            new PagedResponse<ApiKeyResponse>(items, page.TotalCount, page.Page, page.PageSize));
    }

    /// <summary>
    /// Best-effort owner label for the admin list. A principal that no longer resolves
    /// (deleted owner, purged local account) must not hide its key from the admin who
    /// needs to revoke it, so this degrades to null rather than failing the query.
    /// </summary>
    private async Task<string?> ResolveDisplayNameAsync(ApiKey key, CancellationToken cancellationToken)
    {
        if (key.PrincipalType == ApiKeyPrincipalType.LocalUser)
        {
            var local = await localUserRepository.GetByIdAsync(key.PrincipalId, cancellationToken).ConfigureAwait(false);
            return local.IsSuccess ? local.Value!.DisplayName : null;
        }

        var owner = await ownerRepository.GetByIdAsync(key.PrincipalId, cancellationToken).ConfigureAwait(false);
        return owner.IsSuccess ? owner.Value!.DisplayName : null;
    }
}
