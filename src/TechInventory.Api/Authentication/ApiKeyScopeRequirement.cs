using Microsoft.AspNetCore.Authorization;

namespace TechInventory.Api.Authentication;

/// <summary>
/// Confines API-key-authenticated requests to the inventory surface (ADR 0003).
/// </summary>
public sealed class ApiKeyScopeRequirement : IAuthorizationRequirement;

/// <summary>
/// Enforces the ADR 0003 scope table.
/// </summary>
/// <remarks>
/// <para>
/// This handler is attached to every policy, including the default and fallback
/// policies, so no endpoint can escape it by omitting an attribute. It is a
/// <em>no-op for bearer callers</em>: without an <c>apikey_scope</c> claim it
/// immediately succeeds, so Entra and local-JWT authorization is untouched.
/// </para>
/// <para>
/// The allow-list is deliberately an allow-list. A deny-list would silently grant
/// keys access to any endpoint added later, which is exactly the failure mode that
/// keeps API keys out of admin, audit, import, export, report, settings, and
/// key-management routes.
/// </para>
/// </remarks>
public sealed class ApiKeyScopeAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<ApiKeyScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ApiKeyScopeRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scopeClaim = context.User?.FindFirst(ApiKeyAuthenticationHandler.ScopeClaimType)?.Value;
        if (string.IsNullOrEmpty(scopeClaim))
        {
            // Not an API-key request — this requirement has nothing to say.
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var httpContext = context.Resource as HttpContext ?? httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            // Fail closed: without a request we cannot prove the call is in scope.
            return Task.CompletedTask;
        }

        if (ApiKeyScopePolicy.IsWithinScope(scopeClaim, httpContext.Request.Method, httpContext.Request.Path))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
