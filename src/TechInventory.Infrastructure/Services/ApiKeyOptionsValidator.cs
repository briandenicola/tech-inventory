using Microsoft.Extensions.Options;

namespace TechInventory.Infrastructure.Services;

/// <summary>
/// Startup validation for <see cref="ApiKeyOptions"/> (ADR 0003).
/// </summary>
/// <remarks>
/// A missing, malformed, short, or reused pepper is a deployment error, so it
/// fails the host at startup rather than surfacing as a 500 on the first
/// authentication attempt. Paired with <c>ValidateOnStart()</c> in
/// <see cref="DependencyInjection"/>.
/// </remarks>
public sealed class ApiKeyOptionsValidator(IOptions<LocalJwtOptions> localJwtOptions) : IValidateOptions<ApiKeyOptions>
{
    public ValidateOptionsResult Validate(string? name, ApiKeyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var setting = $"{ApiKeyOptions.SectionPath}:{nameof(ApiKeyOptions.HmacPepper)}";

        if (string.IsNullOrWhiteSpace(options.HmacPepper))
        {
            return ValidateOptionsResult.Fail(
                $"{setting} is required. Generate one with `openssl rand -base64 48` and supply it as a deployment secret.");
        }

        var pepper = options.HmacPepper.Trim();

        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(pepper);
        }
        catch (FormatException)
        {
            return ValidateOptionsResult.Fail($"{setting} must be valid base64.");
        }

        if (decoded.Length < ApiKeyOptions.MinimumPepperBytes)
        {
            return ValidateOptionsResult.Fail(
                $"{setting} must decode to at least {ApiKeyOptions.MinimumPepperBytes} bytes " +
                $"({ApiKeyOptions.MinimumPepperBytes * 8} bits); the configured value decodes to {decoded.Length}.");
        }

        // Sharing one secret between JWT signing and key verification means a leak of
        // either compromises both, so the two must never be configured to the same value.
        var signingKey = localJwtOptions.Value.SigningKey;
        if (!string.IsNullOrWhiteSpace(signingKey)
            && string.Equals(signingKey.Trim(), pepper, StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Fail(
                $"{setting} must not be the same value as {LocalJwtOptions.SectionPath}:{nameof(LocalJwtOptions.SigningKey)}.");
        }

        return ValidateOptionsResult.Success;
    }
}
