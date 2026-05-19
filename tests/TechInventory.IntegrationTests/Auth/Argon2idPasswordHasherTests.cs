using FluentAssertions;
using Microsoft.Extensions.Options;
using TechInventory.Infrastructure.Services;

namespace TechInventory.IntegrationTests.Auth;

/// <summary>
/// F025 — guarantee Argon2id round-trip behaviour, since the hasher is the
/// single source of truth between login and change-password. Uses the cheapest
/// parameters that still exercise the real Konscious code path so the suite
/// stays fast.
/// </summary>
public sealed class Argon2idPasswordHasherTests
{
    private static Argon2idPasswordHasher CreateHasher() =>
        new(Options.Create(new Argon2idOptions
        {
            MemoryKib = 1024,
            Iterations = 1,
            Parallelism = 1
        }));

    [Fact]
    public void Hash_thenVerify_RoundTripsForCorrectPassword()
    {
        var hasher = CreateHasher();
        var encoded = hasher.Hash("correct horse battery staple");

        hasher.Verify("correct horse battery staple", encoded, hasher.CurrentAlgorithm).Should().BeTrue();
        hasher.Verify("incorrect", encoded, hasher.CurrentAlgorithm).Should().BeFalse();
    }

    [Fact]
    public void Hash_IsDistinctPerCall_DueToRandomSalt()
    {
        var hasher = CreateHasher();
        var first = hasher.Hash("same-password");
        var second = hasher.Hash("same-password");

        first.Should().NotBe(second);
        hasher.Verify("same-password", first, hasher.CurrentAlgorithm).Should().BeTrue();
        hasher.Verify("same-password", second, hasher.CurrentAlgorithm).Should().BeTrue();
    }

    [Fact]
    public void Verify_ReturnsFalse_ForUnknownAlgorithmTag()
    {
        var hasher = CreateHasher();
        var encoded = hasher.Hash("any");

        hasher.Verify("any", encoded, "scrypt-v1").Should().BeFalse();
    }

    [Theory]
    [InlineData("not-an-argon-string")]
    [InlineData("$argon2id$v=19$m=1024,t=1,p=1$invalid$invalid")]
    [InlineData("$argon2i$v=19$m=1024,t=1,p=1$AAAA$BBBB")]
    public void Verify_ReturnsFalse_ForTamperedEncodedHash(string encoded)
    {
        var hasher = CreateHasher();
        hasher.Verify("password", encoded, hasher.CurrentAlgorithm).Should().BeFalse();
    }

    [Fact]
    public void CurrentAlgorithm_StableTag()
    {
        var hasher = CreateHasher();
        hasher.CurrentAlgorithm.Should().Be("argon2id-v19");
    }
}
