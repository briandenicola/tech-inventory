using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TechInventory.Application.Abstractions.Services;

namespace TechInventory.Infrastructure.Services;

/// <summary>
/// HMAC-SHA-256 verifier for API key secrets, keyed by the deployment pepper (ADR 0003).
/// </summary>
/// <remarks>
/// Registered as a singleton: the pepper is read once at construction, so a
/// missing or too-short pepper fails at startup rather than on the first
/// authentication attempt. See <see cref="ApiKeyOptionsValidator"/> for the
/// startup validation that produces the friendlier message.
/// </remarks>
public sealed class HmacApiKeyHasher : IApiKeyHasher
{
    private readonly byte[] _pepper;
    private readonly string _dummyVerifier;

    public HmacApiKeyHasher(IOptions<ApiKeyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _pepper = DecodePepper(options.Value.HmacPepper);

        // A real verifier over a secret nobody holds. Comparing against this on the
        // unknown-selector path keeps that branch's work identical to a real miss,
        // and it can never accidentally match a presented secret.
        _dummyVerifier = ComputeVerifier("api-key-dummy-verifier-target");
    }

    public string ComputeVerifier(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        return Convert.ToBase64String(HMACSHA256.HashData(_pepper, Encoding.UTF8.GetBytes(secret)));
    }

    public bool VerifyConstantTime(string presentedSecret, string expectedVerifier)
    {
        if (string.IsNullOrWhiteSpace(presentedSecret) || string.IsNullOrWhiteSpace(expectedVerifier))
        {
            // Still burn the same comparison so a malformed presentation is not
            // distinguishable from a wrong one.
            return VerifyDummyConstantTime(presentedSecret ?? string.Empty);
        }

        return FixedTimeCompare(presentedSecret, expectedVerifier);
    }

    public bool VerifyDummyConstantTime(string presentedSecret)
    {
        // Deliberately ignores the outcome: this overload exists purely so the
        // unknown-selector path performs the same HMAC + FixedTimeEquals work.
        FixedTimeCompare(string.IsNullOrEmpty(presentedSecret) ? "\0" : presentedSecret, _dummyVerifier);
        return false;
    }

    private bool FixedTimeCompare(string presentedSecret, string expectedVerifier)
    {
        var computed = Encoding.UTF8.GetBytes(ComputeVerifier(presentedSecret));
        var expected = Encoding.UTF8.GetBytes(expectedVerifier);
        return CryptographicOperations.FixedTimeEquals(computed, expected);
    }

    private static byte[] DecodePepper(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException(
                $"{ApiKeyOptions.SectionPath}:{nameof(ApiKeyOptions.HmacPepper)} is required. " +
                "Generate one with `openssl rand -base64 48` and supply it as a deployment secret.");
        }

        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(configured.Trim());
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                $"{ApiKeyOptions.SectionPath}:{nameof(ApiKeyOptions.HmacPepper)} must be valid base64.", exception);
        }

        if (decoded.Length < ApiKeyOptions.MinimumPepperBytes)
        {
            throw new InvalidOperationException(
                $"{ApiKeyOptions.SectionPath}:{nameof(ApiKeyOptions.HmacPepper)} must decode to at least " +
                $"{ApiKeyOptions.MinimumPepperBytes} bytes ({ApiKeyOptions.MinimumPepperBytes * 8} bits); " +
                $"the configured value decodes to {decoded.Length}.");
        }

        return decoded;
    }
}
