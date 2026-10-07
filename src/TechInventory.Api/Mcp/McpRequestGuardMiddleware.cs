using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace TechInventory.Api.Mcp;

public sealed class McpRequestGuardMiddleware(
    RequestDelegate next,
    IOptions<McpOptions> options,
    ILogger<McpRequestGuardMiddleware> logger)
{
    private const string McpPath = "/api/mcp";

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Request.Path.Equals(McpPath, StringComparison.OrdinalIgnoreCase))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        context.Response.Headers[HeaderNames.CacheControl] = "no-store";

        if (!options.Value.Enabled)
        {
            logger.LogWarning("Rejected MCP request because the MCP endpoint is disabled.");
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(
                new { title = "MCP endpoint disabled", status = StatusCodes.Status503ServiceUnavailable },
                context.RequestAborted).ConfigureAwait(false);
            return;
        }

        var maxRequestBodyBytes = options.Value.MaxRequestBodyBytes;
        if (context.Request.ContentLength > maxRequestBodyBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        var bodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bodySizeFeature is { IsReadOnly: false })
        {
            bodySizeFeature.MaxRequestBodySize = maxRequestBodyBytes;
        }

        await next(context).ConfigureAwait(false);
    }
}
