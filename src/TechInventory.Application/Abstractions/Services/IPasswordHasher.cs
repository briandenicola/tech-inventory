namespace TechInventory.Application.Abstractions.Services;

/// <summary>
/// F025 — password hashing boundary. Implementations are responsible for picking
/// a constant-time verification path and storing the algorithm tag so the system
/// can migrate hashes forward (e.g. raise Argon2 memory cost) without breaking
/// existing credentials.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Stable identifier of the algorithm the hasher will use for newly issued
    /// hashes (e.g. <c>argon2id-v19</c>). Persisted alongside the hash to support
    /// later cost upgrades / algorithm migrations.
    /// </summary>
    string CurrentAlgorithm { get; }

    /// <summary>
    /// Hash <paramref name="password"/> with the current parameters. The returned
    /// encoded string is opaque to callers — it round-trips through
    /// <see cref="Verify"/>.
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// Constant-time verification. Returns <c>false</c> for any algorithm tag the
    /// implementation does not recognise (defense in depth against tampered rows).
    /// </summary>
    bool Verify(string password, string encodedHash, string algorithm);
}
