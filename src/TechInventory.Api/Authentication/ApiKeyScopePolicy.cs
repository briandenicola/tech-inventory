using Microsoft.AspNetCore.Http;
using TechInventory.Application.ApiKeys;

namespace TechInventory.Api.Authentication;

/// <summary>
/// The single source of truth for what an API key may reach (ADR 0003).
/// </summary>
/// <remarks>
/// Used by both enforcement points — <see cref="ApiKeyScopeMiddleware"/> and
/// <see cref="ApiKeyScopeAuthorizationHandler"/> — so the allow-list cannot drift
/// between them.
/// </remarks>
public static class ApiKeyScopePolicy
{
    /// <summary>Collections an <c>inventory.read</c> key may GET.</summary>
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

    private const string DeviceCollection = "/api/v1/devices";

    /// <summary>
    /// Whether a key holding <paramref name="scopeClaim"/> may perform this request.
    /// </summary>
    /// <remarks>
    /// An allow-list, deliberately. A deny-list would silently grant keys access to
    /// every endpoint added after this was written — which is precisely how a key would
    /// end up reaching admin, audit, import, export, report, or settings routes.
    /// </remarks>
    public static bool IsWithinScope(string scopeClaim, string method, PathString path)
    {
        if (!path.HasValue || string.IsNullOrEmpty(scopeClaim))
        {
            return false;
        }

        var value = path.Value!;

        // Key management is bearer-only: a key must never mint or revoke keys, its own
        // included (N-10). Checked first so no prefix below can admit it.
        if (value.StartsWith("/api/v1/api-keys", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!TryMatchCollection(value, out var collection))
        {
            return false;
        }

        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method))
        {
            return true;
        }

        // Writes are devices-only, and only for a write-scoped key. Reference data stays
        // read-only for keys. PATCH is deliberately absent — ADR 0003 grants POST, PUT
        // and DELETE, and nothing wider.
        var isWriteMethod = HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsDelete(method);

        return isWriteMethod
            && string.Equals(collection, DeviceCollection, StringComparison.OrdinalIgnoreCase)
            && string.Equals(scopeClaim, ApiKeyScopeNames.Write, StringComparison.Ordinal);
    }

    /// <summary>
    /// Matches on segment boundaries, so <c>/api/v1/devices-secret</c> cannot pass as
    /// <c>/api/v1/devices</c>.
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
}

/// <summary>
/// Enforces the API key scope for every request, whatever authorization metadata the
/// endpoint declares.
/// </summary>
/// <remarks>
/// <para>
/// This exists because an authorization <em>requirement</em> is not sufficient on its
/// own. <c>[Authorize(Roles = "...")]</c> builds an ad-hoc policy from the attribute
/// that does <b>not</b> include the configured default policy's requirements — so
/// <see cref="ApiKeyScopeRequirement"/> never runs on controllers declared that way,
/// and <c>SettingsController</c> and <c>ReportsController</c> are declared exactly
/// that way. An integration test caught API keys reaching both.
/// </para>
/// <para>
/// Middleware sidesteps the problem: it runs for every authenticated request before
/// authorization, regardless of how the endpoint declares its rules, and denies by
/// default. The requirement is kept as a second layer for policy-based endpoints.
/// </para>
/// </remarks>
public sealed class ApiKeyScopeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scopeClaim = context.User?.FindFirst(ApiKeyAuthenticationHandler.ScopeClaimType)?.Value;

        // No scope claim means this is not an API-key request; bearer auth is untouched.
        if (string.IsNullOrEmpty(scopeClaim))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (ApiKeyScopePolicy.IsWithinScope(scopeClaim, context.Request.Method, context.Request.Path))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(
            ApiKeyProblemDetailsFactory.Create(
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "This API key is not permitted to perform that operation.",
                "Forbidden",
                context.TraceIdentifier),
            context.RequestAborted).ConfigureAwait(false);
    }
}
