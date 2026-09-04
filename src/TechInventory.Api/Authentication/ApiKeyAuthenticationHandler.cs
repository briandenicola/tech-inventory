using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Abstractions.Services;
using TechInventory.Domain.Entities;
using TechInventory.Application.ApiKeys;
using TechInventory.Domain.Enums;

namespace TechInventory.Api.Authentication;

/// <summary>
/// Authenticates <c>Authorization: ApiKey &lt;selector&gt;.&lt;secret&gt;</c> (ADR 0003).
/// </summary>
/// <remarks>
/// <para>
/// Two properties matter more than anything else here and are easy to break by
/// "tidying" the code:
/// </para>
/// <para>
/// <b>1. Every failure looks the same.</b> Unknown selector, wrong secret, expired,
/// revoked, deactivated principal, demoted role — all return the identical
/// <c>401 InvalidApiKey</c>. Adding a more helpful message for any one of them
/// turns this endpoint into an oracle for enumerating valid selectors.
/// </para>
/// <para>
/// <b>2. Both branches do the same cryptographic work.</b> On a lookup miss the
/// handler still runs an HMAC and a fixed-time comparison via
/// <see cref="IApiKeyHasher.VerifyDummyConstantTime"/> before failing. An early
/// <c>return</c> on the miss path would leak "this selector exists" through timing.
/// This is verified by a call-count test, never by measuring elapsed time.
/// </para>
/// <para>
/// The principal is re-resolved from its repository on <em>every</em> request rather
/// than trusted from the key row, so deactivating an account or demoting it to Viewer
/// takes effect immediately for keys already in the wild.
/// </para>
/// </remarks>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IApiKeyRepository apiKeyRepository,
    IApiKeyHasher apiKeyHasher,
    IOwnerRepository ownerRepository,
    ILocalUserRepository localUserRepository,
    TimeProvider timeProvider)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, loggerFactory, encoder)
{
    public const string SchemePrefix = "ApiKey ";
    public const string SelectorClaimType = "apikey_selector";
    public const string ScopeClaimType = "apikey_scope";

    /// <summary>The single failure message every rejection uses. Do not specialise it.</summary>
    private const string InvalidApiKeyMessage = "The API key is invalid or has been revoked.";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith(SchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var credential = header[SchemePrefix.Length..].Trim();
        var separator = credential.IndexOf('.', StringComparison.Ordinal);
        if (separator <= 0 || separator == credential.Length - 1)
        {
            // Malformed: no selector/secret split. Burn a dummy comparison anyway so a
            // malformed presentation costs the same as a well-formed miss.
            apiKeyHasher.VerifyDummyConstantTime(credential.Length == 0 ? "\0" : credential);
            Logger.LogWarning("API key authentication failed: malformed credential (no selector separator).");
            return Fail();
        }

        var selector = credential[..separator];
        var secret = credential[(separator + 1)..];

        var keyResult = await apiKeyRepository.GetBySelectorAsync(selector, Context.RequestAborted).ConfigureAwait(false);
        if (keyResult.IsFailure)
        {
            // MUST NOT early-return before this call — see the remarks above.
            apiKeyHasher.VerifyDummyConstantTime(secret);
            Logger.LogWarning("API key authentication failed for selector {ApiKeySelector}: unknown selector.", selector);
            return Fail();
        }

        var apiKey = keyResult.Value!;

        if (!apiKeyHasher.VerifyConstantTime(secret, apiKey.VerifierHash))
        {
            Logger.LogWarning("API key authentication failed for selector {ApiKeySelector}: verifier mismatch.", selector);
            return Fail();
        }

        var now = timeProvider.GetUtcNow();
        if (!apiKey.IsActive(now))
        {
            Logger.LogWarning(
                "API key authentication failed for selector {ApiKeySelector}: key is revoked or expired.", selector);
            return Fail();
        }

        var principal = await ResolvePrincipalAsync(apiKey).ConfigureAwait(false);
        if (principal is null)
        {
            Logger.LogWarning(
                "API key authentication failed for selector {ApiKeySelector}: principal missing, inactive, or insufficiently privileged.",
                selector);
            return Fail();
        }

        Logger.LogInformation(
            "API key authentication succeeded for selector {ApiKeySelector} (principal {ApiKeyPrincipalId}, scope {ApiKeyScope}).",
            selector,
            apiKey.PrincipalId,
            apiKey.Scope);

        var identity = new ClaimsIdentity(BuildClaims(apiKey, principal.Value), Scheme.Name, "name", ClaimTypes.Role);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, Scheme.Name));
    }

    private static List<Claim> BuildClaims(ApiKey apiKey, (string DisplayName, OwnerRole Role) principal)
    {
        var principalId = apiKey.PrincipalId.ToString();

        // Same claim shape the Entra and Local bearer handlers produce, so the existing
        // Admin / AdminOrMember policies and ICurrentUserService work unchanged.
        var claims = new List<Claim>
        {
            new("sub", principalId),
            new("oid", principalId),
            new("name", principal.DisplayName),
            new(ClaimTypes.Role, principal.Role.ToString()),
            new(SelectorClaimType, apiKey.Selector),
            new(ScopeClaimType, ApiKeyScopeNames.ToWireValue(apiKey.Scope)),
        };

        if (apiKey.PrincipalType == ApiKeyPrincipalType.LocalUser)
        {
            claims.Add(new Claim("auth_method", "local"));
        }

        return claims;
    }

    /// <summary>
    /// Re-reads the key's principal and applies the live ceiling. Returns null when the
    /// principal no longer exists, is deactivated, or has been demoted to a role that
    /// cannot hold keys.
    /// </summary>
    private async Task<(string DisplayName, OwnerRole Role)?> ResolvePrincipalAsync(ApiKey apiKey)
    {
        if (apiKey.PrincipalType == ApiKeyPrincipalType.LocalUser)
        {
            var localResult = await localUserRepository
                .GetByIdAsync(apiKey.PrincipalId, Context.RequestAborted).ConfigureAwait(false);

            if (localResult.IsFailure)
            {
                return null;
            }

            var localUser = localResult.Value!;
            return IsPermitted(localUser.IsActive, localUser.Role)
                ? (localUser.DisplayName, localUser.Role)
                : null;
        }

        var ownerResult = await ownerRepository
            .GetByIdAsync(apiKey.PrincipalId, Context.RequestAborted).ConfigureAwait(false);

        if (ownerResult.IsFailure)
        {
            return null;
        }

        var owner = ownerResult.Value!;
        return IsPermitted(owner.IsActive, owner.Role)
            ? (owner.DisplayName, owner.Role)
            : null;
    }

    /// <summary>
    /// A key is only as privileged as its principal is right now. Demotion to Viewer
    /// revokes it in effect, matching who was allowed to create it in the first place.
    /// </summary>
    private static bool IsPermitted(bool isActive, OwnerRole role)
        => isActive && role is OwnerRole.Admin or OwnerRole.Member;

    private static AuthenticateResult Fail() => AuthenticateResult.Fail(InvalidApiKeyMessage);

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/problem+json";
        await Response.WriteAsJsonAsync(
            ApiKeyProblemDetailsFactory.Create(
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                InvalidApiKeyMessage,
                ApiKeyProblemDetailsFactory.InvalidApiKeyCode,
                Context.TraceIdentifier),
            Context.RequestAborted).ConfigureAwait(false);
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        Response.ContentType = "application/problem+json";
        await Response.WriteAsJsonAsync(
            ApiKeyProblemDetailsFactory.Create(
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "This API key is not permitted to perform that operation.",
                "Forbidden",
                Context.TraceIdentifier),
            Context.RequestAborted).ConfigureAwait(false);
    }
}

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions;

/// <summary>
/// RFC 7807 bodies for the auth pipeline, which runs before MVC's ProblemDetails
/// factory is available.
/// </summary>
public static class ApiKeyProblemDetailsFactory
{
    public const string InvalidApiKeyCode = "InvalidApiKey";
    public const string AmbiguousCredentialCode = "AmbiguousCredential";

    public static Dictionary<string, object?> Create(int status, string title, string detail, string code, string traceId)
        => new(StringComparer.Ordinal)
        {
            ["type"] = status == StatusCodes.Status401Unauthorized
                ? "https://tools.ietf.org/html/rfc7235#section-3.1"
                : "https://tools.ietf.org/html/rfc9110#section-15.5.4",
            ["title"] = title,
            ["status"] = status,
            ["detail"] = detail,
            ["code"] = code,
            ["traceId"] = traceId,
            ["instance"] = null,
        };

    public static string FormatRetryAfter(TimeSpan window)
        => Math.Max(1, (int)Math.Ceiling(window.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
}
