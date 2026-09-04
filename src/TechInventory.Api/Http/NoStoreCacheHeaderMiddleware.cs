using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace TechInventory.Api.Http;

/// <summary>
/// Stamps <c>Cache-Control: no-store</c> on every <c>/api/v1/*</c> response.
/// </summary>
/// <remarks>
/// <para>
/// The API previously sent no cache directives at all. With neither
/// <c>Cache-Control</c> nor a validator (<c>ETag</c>/<c>Last-Modified</c>), a
/// browser or intermediary is free to apply heuristic freshness and serve a
/// device list from its own store — so a user could see their own edit missing
/// even with the service worker behaving correctly.
/// </para>
/// <para>
/// <c>no-store</c> rather than <c>no-cache</c>: these responses carry one
/// household's inventory behind a bearer token or an API key, and there is no
/// revalidation story worth keeping a copy for. Nothing here is cacheable often
/// enough to pay for the risk of a shared cache holding it.
/// </para>
/// <para>
/// This does not disable the PWA's offline copy. Workbox stores responses through
/// the Cache Storage API, which is written explicitly by the service worker and is
/// not governed by these headers — so <c>NetworkFirst</c> still falls back to
/// cached device data when the network is gone (<c>workboxConfig.ts</c>).
/// </para>
/// </remarks>
public sealed class NoStoreCacheHeaderMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Request.Path.StartsWithSegments("/api/v1", StringComparison.OrdinalIgnoreCase))
        {
            // OnStarting, not a direct assignment: headers must be set before the
            // response begins, and an endpoint that streams (export downloads) may
            // start writing before control returns here.
            context.Response.OnStarting(static state =>
            {
                var httpContext = (HttpContext)state;
                httpContext.Response.Headers[HeaderNames.CacheControl] = "no-store";
                return Task.CompletedTask;
            }, context);
        }

        await next(context).ConfigureAwait(false);
    }
}
