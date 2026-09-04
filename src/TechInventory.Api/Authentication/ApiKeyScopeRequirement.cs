using Microsoft.AspNetCore.Authorization;
using TechInventory.Domain.Enums;

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
    /// <summary>Reference data an <c>inventory.read</c> key may GET.</summary>
    private static readonly string[] ReadableCollections =
    [
        "/api/v1/devices",
        "/api/v1/brands",
        "/api/v1/categories",
        "/api/v1/locations",
        "/api/v1/networks",
        "/api/v1/owners",
        "/api/v1/tags",
    ];

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

        if (IsWithinScope(scopeClaim, httpContext.Request.Method, httpContext.Request.Path))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    internal static bool IsWithinScope(string scopeClaim, string method, PathString path)
    {
        if (!path.HasValue)
        {
            return false;
        }

        var value = path.Value!;

        // Key management is bearer-only: a key must never be able to mint or revoke
        // keys, including its own (N-10). Checked before anything else so no
        // collection prefix below can accidentally admit it.
        if (value.StartsWith("/api/v1/api-keys", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!TryMatchCollection(value, out var collection))
        {
            return false;
        }

        var isDeviceRoute = string.Equals(collection, "/api/v1/devices", StringComparison.OrdinalIgnoreCase);

        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method))
        {
            return true;
        }

        // Writes are devices-only, and only for a write-scoped key. Reference data stays
        // read-only for keys: renaming a brand from a script is not what these are for.
        // PATCH is deliberately absent — ADR 0003 grants POST/PUT/DELETE and nothing else.
        var isWriteMethod = HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsDelete(method);

        return isDeviceRoute
            && isWriteMethod
            && string.Equals(scopeClaim, ApiKeyScopeNames.Write, StringComparison.Ordinal);
    }

    /// <summary>
    /// Matches a path against the allow-list on segment boundaries, so
    /// <c>/api/v1/devices-secret</c> cannot pass as <c>/api/v1/devices</c>.
    /// </summary>
    private static bool TryMatchCollection(string path, out string collection)
    {
        foreach (var candidate in ReadableCollections)
        {
            if (path.Equals(candidate, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(candidate + "/", StringComparison.OrdinalIgnoreCase))
            {
                collection = candidate;
                return true;
            }
        }

        collection = string.Empty;
        return false;
    }

    /// <summary>Maps the enum to its claim value; used by tests and the handler alike.</summary>
    internal static string ToClaimValue(ApiKeyScope scope) => ApiKeyScopeNames.ToClaimValue(scope);
}
