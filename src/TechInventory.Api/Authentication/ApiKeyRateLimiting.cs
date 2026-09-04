using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace TechInventory.Api.Authentication;

/// <summary>
/// Per-selector rate limiting for API-key traffic (ADR 0003).
/// </summary>
/// <remarks>
/// Partitioned by selector, not by principal: one noisy integration should not
/// exhaust the budget of the owner's other keys. Limits are bound to configuration
/// specifically so integration tests can dial the window down to a couple of
/// requests — asserting a 429 by firing 60 requests at the production default would
/// be slow and flaky.
/// </remarks>
public static class ApiKeyRateLimiting
{
    public const string PolicyName = "ApiKeyPerSelector";
    public const string ConfigurationSection = "RateLimiting:ApiKey";

    public static RateLimitPartition<string> CreatePartition(HttpContext httpContext, ApiKeyRateLimitOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);

        var selector = httpContext.User?.FindFirst(ApiKeyAuthenticationHandler.SelectorClaimType)?.Value;

        // Bearer callers are not rate limited by this policy — they are already bounded
        // by interactive sign-in — so they share one effectively unlimited partition.
        if (string.IsNullOrEmpty(selector))
        {
            return RateLimitPartition.GetNoLimiter("bearer");
        }

        return RateLimitPartition.GetFixedWindowLimiter(selector, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = options.PermitLimit,
            Window = TimeSpan.FromSeconds(options.WindowSeconds),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });
    }
}

public sealed class ApiKeyRateLimitOptions
{
    public const string SectionPath = ApiKeyRateLimiting.ConfigurationSection;

    public int PermitLimit { get; set; } = 60;

    public int WindowSeconds { get; set; } = 60;
}
