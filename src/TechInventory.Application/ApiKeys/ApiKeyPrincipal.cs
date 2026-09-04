using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Abstractions.Services;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.ApiKeys;

/// <summary>The identity a key is issued to, or acts on behalf of.</summary>
public sealed record ApiKeyPrincipal(ApiKeyPrincipalType Type, Guid Id, string DisplayName, OwnerRole Role, bool IsActive);

/// <summary>
/// Resolves the authenticated caller to the <see cref="ApiKeyPrincipalType"/> and
/// domain id a key should be stored against.
/// </summary>
/// <remarks>
/// An Entra caller presents an Entra object id, which is <em>not</em> the
/// <c>Owner.Id</c> the key must reference — it has to be translated through
/// <see cref="IOwnerRepository.GetByEntraObjectIdAsync"/>. A local caller presents
/// the <c>LocalUser.Id</c> directly. Getting this wrong would store a key against
/// an id that no repository can resolve, so the translation lives in one place.
/// </remarks>
public sealed class ApiKeyPrincipalResolver(
    ICurrentUserService currentUserService,
    IOwnerRepository ownerRepository,
    ILocalUserRepository localUserRepository)
{
    public const string LocalAuthenticationMethod = "local";

    public async Task<Result<ApiKeyPrincipal>> ResolveCurrentAsync(CancellationToken cancellationToken)
    {
        var subject = currentUserService.GetCurrentUserId();
        if (!Guid.TryParse(subject, out var subjectId))
        {
            return Result<ApiKeyPrincipal>.Failure(
                Error.Forbidden("API keys can only be created by a signed-in household member."));
        }

        var isLocal = string.Equals(
            currentUserService.GetAuthenticationMethod(),
            LocalAuthenticationMethod,
            StringComparison.Ordinal);

        return isLocal
            ? await ResolveLocalUserAsync(subjectId, cancellationToken).ConfigureAwait(false)
            : await ResolveOwnerAsync(subjectId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<ApiKeyPrincipal>> ResolveLocalUserAsync(Guid localUserId, CancellationToken cancellationToken)
    {
        var result = await localUserRepository.GetByIdAsync(localUserId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Result<ApiKeyPrincipal>.Failure(
                Error.Forbidden("API keys can only be created by a signed-in household member."));
        }

        var user = result.Value!;
        return Result<ApiKeyPrincipal>.Success(
            new ApiKeyPrincipal(ApiKeyPrincipalType.LocalUser, user.Id, user.DisplayName, user.Role, user.IsActive));
    }

    private async Task<Result<ApiKeyPrincipal>> ResolveOwnerAsync(Guid entraObjectId, CancellationToken cancellationToken)
    {
        var result = await ownerRepository.GetByEntraObjectIdAsync(entraObjectId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Result<ApiKeyPrincipal>.Failure(
                Error.Forbidden("Your account is not linked to a household owner, so it cannot hold API keys."));
        }

        var owner = result.Value!;
        return Result<ApiKeyPrincipal>.Success(
            new ApiKeyPrincipal(ApiKeyPrincipalType.Owner, owner.Id, owner.DisplayName, owner.Role, owner.IsActive));
    }
}
