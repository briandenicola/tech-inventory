using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;
using TechInventory.Application.Abstractions.Services;

namespace TechInventory.Infrastructure.Services;

/// <summary>
/// F025 — Argon2id password hasher (OWASP 2025 baseline parameters).
///
/// Encoded format: <c>$argon2id$v=19$m={kib},t={iterations},p={parallelism}$salt$hash</c>
/// where salt + hash are base64 url-encoded. The format is independent of the
/// PHC string format on purpose: we never round-trip arbitrary external hashes,
/// and the explicit parameter line makes audit + future-migration trivial.
///
/// Parameter cost is read from configuration so the operator can raise the bar
/// on stronger hardware (Pi → mini-PC → real server) without redeploying code.
/// </summary>
public sealed class Argon2idPasswordHasher(IOptions<Argon2idOptions> options) : IPasswordHasher
{
    public const string AlgorithmTag = "argon2id-v19";
    private const int SaltLengthBytes = 16;
    private const int HashLengthBytes = 32;

    private readonly Argon2idOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public string CurrentAlgorithm => AlgorithmTag;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltLengthBytes);
        var hashBytes = DeriveHash(password, salt, _options.MemoryKib, _options.Iterations, _options.Parallelism);
        return EncodeHash(_options.MemoryKib, _options.Iterations, _options.Parallelism, salt, hashBytes);
    }

    public bool Verify(string password, string encodedHash, string algorithm)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(encodedHash))
        {
            return false;
        }

        // Only honour algorithm tags we recognise. Tampered rows with an unknown
        // tag fail closed even if the rest of the encoded blob is well-formed.
        if (!string.Equals(algorithm, AlgorithmTag, StringComparison.Ordinal))
        {
            return false;
        }

        if (!TryDecodeHash(encodedHash, out var memoryKib, out var iterations, out var parallelism, out var salt, out var expected))
        {
            return false;
        }

        var actual = DeriveHash(password, salt, memoryKib, iterations, parallelism);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] DeriveHash(string password, byte[] salt, int memoryKib, int iterations, int parallelism)
    {
        using var argon = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memoryKib,
            Iterations = iterations,
            DegreeOfParallelism = parallelism
        };
        return argon.GetBytes(HashLengthBytes);
    }

    private static string EncodeHash(int memoryKib, int iterations, int parallelism, byte[] salt, byte[] hash)
    {
        return string.Create(System.Globalization.CultureInfo.InvariantCulture, $"$argon2id$v=19$m={memoryKib},t={iterations},p={parallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}");
    }

    private static bool TryDecodeHash(string encoded, out int memoryKib, out int iterations, out int parallelism, out byte[] salt, out byte[] hash)
    {
        memoryKib = iterations = parallelism = 0;
        salt = Array.Empty<byte>();
        hash = Array.Empty<byte>();

        var parts = encoded.Split('$');
        // Empty-leading segment + 5 fields → 6 parts. Reject anything else.
        if (parts.Length != 6 || parts[1] != "argon2id" || parts[2] != "v=19")
        {
            return false;
        }

        var paramSection = parts[3].Split(',');
        if (paramSection.Length != 3)
        {
            return false;
        }

        if (!TryParseParam(paramSection[0], "m=", out memoryKib) ||
            !TryParseParam(paramSection[1], "t=", out iterations) ||
            !TryParseParam(paramSection[2], "p=", out parallelism))
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[4]);
            hash = Convert.FromBase64String(parts[5]);
        }
        catch (FormatException)
        {
            return false;
        }

        return true;
    }

    private static bool TryParseParam(string segment, string prefix, out int value)
    {
        if (!segment.StartsWith(prefix, StringComparison.Ordinal))
        {
            value = 0;
            return false;
        }
        return int.TryParse(segment.AsSpan(prefix.Length), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out value) && value > 0;
    }
}

/// <summary>
/// Bound to <c>Auth:Local:Argon2:*</c>. Defaults match the OWASP 2025
/// "low-memory" recommendation (19 MiB, 2 iterations, parallelism 1) so the
/// system stays usable on Pi-class hardware.
/// </summary>
public sealed class Argon2idOptions
{
    public const string SectionPath = "Auth:Local:Argon2";

    public int MemoryKib { get; set; } = 19 * 1024;

    public int Iterations { get; set; } = 2;

    public int Parallelism { get; set; } = 1;
}
