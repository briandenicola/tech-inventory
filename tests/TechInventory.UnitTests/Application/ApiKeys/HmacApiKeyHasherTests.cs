using FluentAssertions;
using Microsoft.Extensions.Options;
using TechInventory.Infrastructure.Services;

namespace TechInventory.UnitTests.Application.ApiKeys;

public class HmacApiKeyHasherTests
{
    // 48 bytes of base64. Test-only.
    private const string ValidPepper = "dGVzdC1vbmx5LWFwaS1rZXktcGVwcGVyLW5vdC1hLXJlYWwtc2VjcmV0LTAxMjM0NTY3ODk=";

    private static HmacApiKeyHasher CreateHasher(string? pepper = ValidPepper)
        => new(Options.Create(new ApiKeyOptions { HmacPepper = pepper }));

    [Fact]
    public void ComputeVerifier_IsDeterministicForTheSameSecret()
    {
        var hasher = CreateHasher();

        hasher.ComputeVerifier("secret-value").Should().Be(hasher.ComputeVerifier("secret-value"));
    }

    [Fact]
    public void ComputeVerifier_DiffersBetweenSecrets()
    {
        var hasher = CreateHasher();

        hasher.ComputeVerifier("secret-a").Should().NotBe(hasher.ComputeVerifier("secret-b"));
    }

    [Fact]
    public void ComputeVerifier_DependsOnThePepper()
    {
        // The whole point of the pepper: the same secret must not produce the same
        // verifier under a different deployment secret, so a stolen database from one
        // deployment cannot be replayed against another.
        var first = CreateHasher();
        var second = CreateHasher("b3RoZXItcGVwcGVyLXZhbHVlLXRoYXQtaXMtbG9uZy1lbm91Z2gtMDEyMzQ1Njc4OQ==");

        first.ComputeVerifier("secret-value").Should().NotBe(second.ComputeVerifier("secret-value"));
    }

    [Fact]
    public void ComputeVerifier_NeverReturnsTheSecretItself()
    {
        var hasher = CreateHasher();

        hasher.ComputeVerifier("super-secret").Should().NotContain("super-secret");
    }

    [Fact]
    public void VerifyConstantTime_AcceptsTheMatchingSecret()
    {
        var hasher = CreateHasher();
        var verifier = hasher.ComputeVerifier("the-real-secret");

        hasher.VerifyConstantTime("the-real-secret", verifier).Should().BeTrue();
    }

    [Theory]
    [InlineData("wrong-secret")]
    [InlineData("the-real-secre")]
    [InlineData("the-real-secrets")]
    [InlineData("")]
    [InlineData("   ")]
    public void VerifyConstantTime_RejectsAnythingElse(string presented)
    {
        var hasher = CreateHasher();
        var verifier = hasher.ComputeVerifier("the-real-secret");

        hasher.VerifyConstantTime(presented, verifier).Should().BeFalse();
    }

    [Fact]
    public void VerifyDummyConstantTime_AlwaysReturnsFalse()
    {
        // The unknown-selector path must never authenticate, whatever it is handed —
        // including a secret that is valid for some other key.
        var hasher = CreateHasher();

        hasher.VerifyDummyConstantTime("anything").Should().BeFalse();
        hasher.VerifyDummyConstantTime(string.Empty).Should().BeFalse();
        hasher.VerifyDummyConstantTime(hasher.ComputeVerifier("real")).Should().BeFalse();
    }

    [Fact]
    public void VerifyDummyConstantTime_DoesNotThrowOnDegenerateInput()
    {
        // It runs on the failure path, so throwing here would turn a clean 401 into a
        // 500 and re-expose the "does this selector exist?" signal it exists to hide.
        var hasher = CreateHasher();

        var act = () =>
        {
            hasher.VerifyDummyConstantTime(string.Empty);
            hasher.VerifyDummyConstantTime("\0");
            hasher.VerifyDummyConstantTime(new string('x', 4096));
        };

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsAMissingPepper(string? pepper)
    {
        var act = () => CreateHasher(pepper);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*HmacPepper is required*");
    }

    [Fact]
    public void Constructor_RejectsANonBase64Pepper()
    {
        var act = () => CreateHasher("not!valid!base64!");

        act.Should().Throw<InvalidOperationException>().WithMessage("*base64*");
    }

    [Fact]
    public void Constructor_RejectsAPepperShorterThan32Bytes()
    {
        // 16 decoded bytes — well-formed base64, but too little entropy to key an
        // HMAC-SHA-256 verifier.
        var act = () => CreateHasher(Convert.ToBase64String(new byte[16]));

        act.Should().Throw<InvalidOperationException>().WithMessage("*at least 32 bytes*");
    }

    [Fact]
    public void Constructor_AcceptsExactlyThirtyTwoBytes()
    {
        var act = () => CreateHasher(Convert.ToBase64String(new byte[32]));

        act.Should().NotThrow("32 bytes is the documented minimum, not an exclusive bound");
    }
}
