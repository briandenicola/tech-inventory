using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TechInventory.Api.Authentication;

/// <summary>
/// Rejects API-key-authenticated callers with 403 (N-10).
/// </summary>
/// <remarks>
/// Applied to key management so a key cannot bootstrap further keys or revoke
/// another member's. Detection is by the presence of the <c>apikey_scope</c> claim,
/// which only <see cref="ApiKeyAuthenticationHandler"/> issues, rather than by the
/// authentication scheme name — the claim travels with the principal and cannot be
/// spoofed by a bearer caller.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RejectApiKeyAuthenticationAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var isApiKeyCaller = context.HttpContext.User?.FindFirst(ApiKeyAuthenticationHandler.ScopeClaimType) is not null;
        if (!isApiKeyCaller)
        {
            return;
        }

        context.Result = new ObjectResult(ApiKeyProblemDetailsFactory.Create(
            StatusCodes.Status403Forbidden,
            "Forbidden",
            "API keys cannot manage API keys. Sign in to create or revoke keys.",
            "Forbidden",
            context.HttpContext.TraceIdentifier))
        {
            StatusCode = StatusCodes.Status403Forbidden,
            ContentTypes = { "application/problem+json" },
        };
    }
}
