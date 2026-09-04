namespace TechInventory.Application.Abstractions.Services;

/// <summary>
/// Computes and checks the stored verifier for an API key's secret half (ADR 0003).
/// </summary>
/// <remarks>
/// Implementations must use a keyed hash under a deployment pepper, so that a
/// database breach alone does not yield forgeable keys, and must compare in
/// data-independent time.
/// </remarks>
public interface IApiKeyHasher
{
    /// <summary>Derives the value stored in <c>ApiKey.VerifierHash</c> for a plaintext secret.</summary>
    string ComputeVerifier(string secret);

    /// <summary>Fixed-time check of a presented secret against a stored verifier.</summary>
    bool VerifyConstantTime(string presentedSecret, string expectedVerifier);

    /// <summary>
    /// Performs the identical HMAC computation and fixed-time comparison against a
    /// fixed internal verifier, then always returns <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// Called on the unknown-selector path so that "this selector exists" and
    /// "this selector does not exist" perform the same cryptographic work and the
    /// difference is not observable (ADR 0003). Callers must never short-circuit
    /// past this on a lookup miss.
    /// </remarks>
    bool VerifyDummyConstantTime(string presentedSecret);
}
