namespace TechInventory.Infrastructure.Services;

/// <summary>
/// Deployment configuration for API key verification (ADR 0003).
/// </summary>
public sealed class ApiKeyOptions
{
    public const string SectionPath = "ApiKeys";

    /// <summary>Minimum decoded pepper length. 256 bits, matching the HMAC-SHA-256 block security level.</summary>
    public const int MinimumPepperBytes = 32;

    /// <summary>
    /// Base64 secret keying the verifier HMAC. Generate with
    /// <c>openssl rand -base64 48</c> and supply as a Docker secret — never commit it.
    /// Must differ from <c>Auth:Local:SigningKey</c>: reusing one secret for both
    /// JWT signing and key verification means a leak of either compromises both.
    /// </summary>
    public string? HmacPepper { get; set; }
}
