using System.Security.Cryptography;
using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Abstractions.Services;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.ApiKeys.Commands;

public sealed record CreateApiKeyCommand(string Name, ApiKeyScope Scope, int? ExpiresInDays = null)
    : IRequest<Result<CreatedApiKeyResponse>>, IAuditable;

public sealed class CreateApiKeyCommandHandler(
    IApiKeyRepository apiKeyRepository,
    IHouseholdRepository householdRepository,
    ApiKeyPrincipalResolver principalResolver,
    IApiKeyHasher apiKeyHasher,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext,
    TimeProvider timeProvider) : IRequestHandler<CreateApiKeyCommand, Result<CreatedApiKeyResponse>>
{
    /// <summary>16 bytes — a public lookup handle, sized for uniqueness, not secrecy.</summary>
    private const int SelectorBytes = 16;

    /// <summary>32 bytes — the actual credential, sized to make guessing infeasible.</summary>
    private const int SecretBytes = 32;

    public async Task<Result<CreatedApiKeyResponse>> Handle(CreateApiKeyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var principalResult = await principalResolver.ResolveCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (principalResult.IsFailure)
        {
            return Result<CreatedApiKeyResponse>.Failure(principalResult.Error!);
        }

        var principal = principalResult.Value!;

        // Viewers are read-only in the UI and are not expected to automate; letting them
        // mint credentials would widen that role by the back door (ADR 0003).
        if (principal.Role == OwnerRole.Viewer)
        {
            return Result<CreatedApiKeyResponse>.Failure(
                Error.Forbidden("Viewers cannot create API keys."));
        }

        if (!principal.IsActive)
        {
            return Result<CreatedApiKeyResponse>.Failure(
                Error.Forbidden("Deactivated accounts cannot create API keys."));
        }

        var now = timeProvider.GetUtcNow();

        var activeCount = await apiKeyRepository
            .CountActiveByPrincipalAsync(principal.Type, principal.Id, now, cancellationToken)
            .ConfigureAwait(false);

        if (activeCount >= ApiKey.MaxActiveKeysPerPrincipal)
        {
            return Result<CreatedApiKeyResponse>.Failure(Error.QuotaExceeded(
                $"You already have {ApiKey.MaxActiveKeysPerPrincipal} active API keys. Revoke one before creating another."));
        }

        var nameTaken = await apiKeyRepository
            .NameExistsForPrincipalAsync(principal.Type, principal.Id, request.Name, cancellationToken)
            .ConfigureAwait(false);

        if (nameTaken)
        {
            return Result<CreatedApiKeyResponse>.Failure(
                Error.Conflict($"You already have an API key named '{request.Name.Trim()}'."));
        }

        var householdResult = await ResolveSingleHouseholdAsync(cancellationToken).ConfigureAwait(false);
        if (householdResult.IsFailure)
        {
            return Result<CreatedApiKeyResponse>.Failure(householdResult.Error!);
        }

        var selector = GenerateUrlSafeToken(SelectorBytes);
        var secret = GenerateUrlSafeToken(SecretBytes);
        var expiresAt = now.AddDays(request.ExpiresInDays ?? ApiKey.DefaultExpiryDays);

        try
        {
            var apiKey = new ApiKey(
                Guid.NewGuid(),
                householdResult.Value!.Id,
                request.Name,
                selector,
                apiKeyHasher.ComputeVerifier(secret),
                request.Scope,
                principal.Type,
                principal.Id,
                expiresAt);

            var addResult = await apiKeyRepository.AddAsync(apiKey, cancellationToken).ConfigureAwait(false);
            if (addResult.IsFailure)
            {
                return Result<CreatedApiKeyResponse>.Failure(addResult.Error!);
            }

            // Explicit payload: the AuditBehavior falls back to serialising the whole
            // request when none is set, and this record must never carry the secret.
            auditContext.Set(new AuditContextEntry(
                nameof(ApiKey),
                apiKey.Id.ToString(),
                AuditAction.Created,
                afterPayload: new
                {
                    apiKey.Selector,
                    apiKey.Name,
                    Scope = apiKey.Scope.ToString(),
                    apiKey.ExpiresAt,
                    PrincipalType = apiKey.PrincipalType.ToString(),
                    apiKey.PrincipalId,
                }));

            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result<CreatedApiKeyResponse>.Success(new CreatedApiKeyResponse(
                apiKey.Id,
                apiKey.Name,
                apiKey.Selector,
                apiKey.Scope,
                apiKey.CreatedAt,
                apiKey.ExpiresAt,
                $"{selector}.{secret}"));
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            return Result<CreatedApiKeyResponse>.Failure(Error.Conflict(exception.Message));
        }
    }

    /// <summary>
    /// base64url without padding — safe in an <c>Authorization</c> header, and free of
    /// the <c>.</c> that separates the selector from the secret.
    /// </summary>
    private static string GenerateUrlSafeToken(int byteCount)
        => Base64UrlEncode(RandomNumberGenerator.GetBytes(byteCount));

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private async Task<Result<Household>> ResolveSingleHouseholdAsync(CancellationToken cancellationToken)
    {
        var households = await householdRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        return households.Count switch
        {
            1 => Result<Household>.Success(households[0]),
            0 => Result<Household>.Failure(Error.Conflict("A household must exist before creating an API key.")),
            _ => Result<Household>.Failure(Error.Conflict("CreateApiKeyCommand requires exactly one household.")),
        };
    }
}
