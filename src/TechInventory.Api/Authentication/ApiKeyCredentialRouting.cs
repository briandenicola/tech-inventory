namespace TechInventory.Api.Authentication;

/// <summary>
/// Credential sniffing for the <c>TechInventoryAuth</c> policy scheme (ADR 0003).
/// </summary>
/// <remarks>
/// Shared by both <c>ForwardDefaultSelector</c> registrations in <c>Program.cs</c>
/// (Entra-configured and pure-local) so the two can never drift apart on which
/// credentials route where.
/// </remarks>
public static class ApiKeyCredentialRouting
{
    private const string ApiKeyPrefix = "ApiKey ";
    private const string BearerPrefix = "Bearer ";

    /// <summary>
    /// True when the request presents both an API key and a bearer token.
    /// </summary>
    /// <remarks>
    /// Covers both shapes this can take: repeated <c>Authorization</c> headers, and a
    /// single comma-joined header value. Checking only the first header value would
    /// let a caller hide a second credential behind a comma.
    /// </remarks>
    public static bool HasAmbiguousCredentials(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sawApiKey = false;
        var sawBearer = false;

        foreach (var headerValue in request.Headers.Authorization)
        {
            if (string.IsNullOrWhiteSpace(headerValue))
            {
                continue;
            }

            foreach (var part in headerValue.Split(','))
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith(ApiKeyPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    sawApiKey = true;
                }
                else if (trimmed.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    sawBearer = true;
                }
            }
        }

        return sawApiKey && sawBearer;
    }

    /// <summary>True when the request presents an API key (and, by the guard above, only an API key).</summary>
    public static bool IsApiKeyRequest(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var header = request.Headers.Authorization.ToString();
        return !string.IsNullOrEmpty(header)
            && header.TrimStart().StartsWith(ApiKeyPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
