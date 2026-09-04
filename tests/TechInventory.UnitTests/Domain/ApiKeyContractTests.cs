using FluentAssertions;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.UnitTests.Domain;

public class ApiKeyContractTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    private static ApiKey CreateKey(
        DateTimeOffset? expiresAt = null,
        ApiKeyScope scope = ApiKeyScope.Read,
        ApiKeyPrincipalType principalType = ApiKeyPrincipalType.Owner,
        Guid? principalId = null,
        string name = "Home Assistant")
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            name,
            "c2VsZWN0b3I",
            "dmVyaWZpZXItaGFzaA==",
            scope,
            principalType,
            principalId ?? Guid.NewGuid(),
            expiresAt ?? DateTimeOffset.UtcNow.AddDays(90));

    [Fact]
    public void ApiKey_CapturesEveryFieldAtCreation()
    {
        var householdId = Guid.NewGuid();
        var principalId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);

        var apiKey = new ApiKey(
            Guid.NewGuid(),
            householdId,
            "  Home Assistant  ",
            "selector-value",
            "verifier-hash",
            ApiKeyScope.Write,
            ApiKeyPrincipalType.LocalUser,
            principalId,
            expiresAt,
            createdBy: "brian");

        apiKey.HouseholdId.Should().Be(householdId);
        apiKey.Name.Should().Be("Home Assistant", "the guard trims surrounding whitespace");
        apiKey.Selector.Should().Be("selector-value");
        apiKey.VerifierHash.Should().Be("verifier-hash");
        apiKey.Scope.Should().Be(ApiKeyScope.Write);
        apiKey.PrincipalType.Should().Be(ApiKeyPrincipalType.LocalUser);
        apiKey.PrincipalId.Should().Be(principalId);
        apiKey.ExpiresAt.Should().Be(expiresAt);
        apiKey.RevokedAt.Should().BeNull();
        apiKey.RevokedBy.Should().BeNull();
        apiKey.CreatedBy.Should().Be("brian");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ApiKey_RequiresAName(string? name)
    {
        var act = () => CreateKey(name: name!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ApiKey_RejectsANameLongerThanTheColumn()
    {
        var act = () => CreateKey(name: new string('a', ApiKey.MaxNameLength + 1));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ApiKey_RejectsAnEmptyPrincipalId()
    {
        var act = () => CreateKey(principalId: Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ApiKey_CanBeReconstructedWithAPastExpiry()
    {
        // Regression guard. EF materialises rows through this constructor, so rejecting
        // a past expiry here made every naturally expired key unreadable — the auth
        // handler threw instead of returning 401, and listing keys would have 500'd.
        // "Expires 1-365 days out" is a creation rule (CreateApiKeyCommandValidator),
        // not an invariant of the type.
        var act = () => CreateKey(expiresAt: DateTimeOffset.UtcNow.AddDays(-1));

        act.Should().NotThrow();
    }

    [Fact]
    public void IsActive_IsTrueBeforeExpiryAndFalseAfter()
    {
        var apiKey = CreateKey(expiresAt: Now.AddDays(1));

        apiKey.IsActive(Now).Should().BeTrue();
        apiKey.IsActive(Now.AddDays(2)).Should().BeFalse("the key has passed its expiry");
    }

    [Fact]
    public void IsActive_IsFalseOnceRevoked_EvenWhileUnexpired()
    {
        var apiKey = CreateKey(expiresAt: Now.AddDays(365));

        apiKey.Revoke("brian", Now);

        apiKey.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void Revoke_RecordsWhenAndByWhom()
    {
        var apiKey = CreateKey();

        apiKey.Revoke("brian", Now);

        apiKey.RevokedAt.Should().Be(Now);
        apiKey.RevokedBy.Should().Be("brian");
    }

    [Fact]
    public void Revoke_IsIdempotentAndKeepsTheFirstRevoker()
    {
        // A retried request, or an admin and the owner acting at once, must not fail
        // the second call or rewrite who actually revoked the key first.
        var apiKey = CreateKey();
        apiKey.Revoke("brian", Now);

        var act = () => apiKey.Revoke("someone-else", Now.AddHours(1));

        act.Should().NotThrow();
        apiKey.RevokedAt.Should().Be(Now);
        apiKey.RevokedBy.Should().Be("brian");
    }

    [Fact]
    public void QuotaAndExpiryConstants_MatchTheApprovedDecision()
    {
        // These are the "90_365_5" product decision on issue #149. Changing any of them
        // is a deliberate product change, not a refactor, so it should break this test.
        ApiKey.DefaultExpiryDays.Should().Be(90);
        ApiKey.MaxExpiryDays.Should().Be(365);
        ApiKey.MaxActiveKeysPerPrincipal.Should().Be(5);
    }
}
