using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TechInventory.Domain.Enums;
using TechInventory.Infrastructure.Persistence;
using static TechInventory.IntegrationTests.ApiKeys.ApiKeyTestSupport;

namespace TechInventory.IntegrationTests.ApiKeys;

/// <summary>
/// Happy-path lifecycle for #149: create, use, revoke, and the quota (US-1..US-5).
/// </summary>
[Collection("ApiKeyUnthrottled")]
public sealed class ApiKeyLifecycleTests(ApiKeyUnthrottledTestHostFactory factory)
    : IClassFixture<ApiKeyUnthrottledTestHostFactory>
{
    private const string AdminUser = "apikey-admin";
    private const string Password = "Str0ng!TestPassword";

    private async Task<HttpClient> SignedInAdminAsync(OwnerRole role = OwnerRole.Admin)
    {
        await ResetDatabaseAsync(factory);
        await EnsureHouseholdAsync(factory);
        await ResetAndSeedLocalUserAsync(factory, role, AdminUser, Password);

        var client = factory.CreateClient();
        UseBearer(client, await LoginAsync(client, AdminUser, Password));
        return client;
    }

    private static async Task<CreatedKeyDto> CreateKeyAsync(
        HttpClient client,
        string name = "Home Assistant",
        string scope = "inventory.read",
        int? expiresInDays = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/api-keys",
            new { name, scope, expiresInDays },
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<CreatedKeyDto>(JsonOptions))!;
    }

    [Fact]
    public async Task CreateKey_ReturnsTheCredentialExactlyOnceAndStoresOnlyAVerifier()
    {
        var client = await SignedInAdminAsync();

        var created = await CreateKeyAsync(client);

        created.Key.Should().StartWith(created.Selector + ".");
        created.Key.Should().NotBe(created.Selector);
        created.Scope.Should().Be("inventory.read");
        created.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(90), TimeSpan.FromMinutes(5));

        var secret = created.Key[(created.Selector.Length + 1)..];

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.ApiKeys.SingleAsync(key => key.Id == created.Id);

        // The row must be useless to an attacker who reads the database.
        stored.VerifierHash.Should().NotBe(secret);
        stored.VerifierHash.Should().NotContain(secret);
        db.ApiKeys.Any(key => key.Selector == secret).Should().BeFalse();
    }

    [Fact]
    public async Task ListKeys_NeverIncludesTheSecretOrVerifier()
    {
        // N-17.
        var client = await SignedInAdminAsync();
        var created = await CreateKeyAsync(client);
        var secret = created.Key[(created.Selector.Length + 1)..];

        var response = await client.GetAsync("/api/v1/api-keys");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(secret, "the plaintext secret must never be retrievable");
        body.Should().NotContain("verifierHash");
        body.Should().Contain(created.Selector, "the selector is public and identifies the key");
    }

    [Fact]
    public async Task ApiKey_AuthenticatesAgainstInventoryEndpoints()
    {
        // US-5.
        var client = await SignedInAdminAsync();
        var created = await CreateKeyAsync(client);

        var keyClient = factory.CreateClient();
        UseApiKey(keyClient, created.Key);

        var response = await keyClient.GetAsync("/api/v1/devices");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokedKey_StopsAuthenticating()
    {
        // US-3 end to end: the key works, is revoked, and immediately stops working.
        var client = await SignedInAdminAsync();
        var created = await CreateKeyAsync(client);

        var keyClient = factory.CreateClient();
        UseApiKey(keyClient, created.Key);
        (await keyClient.GetAsync("/api/v1/devices")).StatusCode.Should().Be(HttpStatusCode.OK);

        var revoke = await client.DeleteAsync($"/api/v1/api-keys/{created.Id}");
        revoke.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await keyClient.GetAsync("/api/v1/devices")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoke_IsIdempotent()
    {
        var client = await SignedInAdminAsync();
        var created = await CreateKeyAsync(client);

        (await client.DeleteAsync($"/api/v1/api-keys/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.DeleteAsync($"/api/v1/api-keys/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateKey_RefusesTheSixthActiveKey()
    {
        // N-15 / quota 5.
        var client = await SignedInAdminAsync();

        for (var i = 1; i <= 5; i++)
        {
            await CreateKeyAsync(client, name: $"key-{i}");
        }

        var sixth = await client.PostAsJsonAsync(
            "/api/v1/api-keys",
            new { name = "key-6", scope = "inventory.read", expiresInDays = (int?)null },
            JsonOptions);

        sixth.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await sixth.Content.ReadAsStringAsync()).Should().Contain("QuotaExceeded");
    }

    [Fact]
    public async Task CreateKey_AllowsANewKeyOnceOneIsRevoked()
    {
        // The quota counts active keys, so revoking must free a slot — otherwise
        // "revoke then recreate" (the documented substitute for rotation) is impossible.
        var client = await SignedInAdminAsync();
        var first = await CreateKeyAsync(client, name: "key-1");
        for (var i = 2; i <= 5; i++)
        {
            await CreateKeyAsync(client, name: $"key-{i}");
        }

        await client.DeleteAsync($"/api/v1/api-keys/{first.Id}");

        var replacement = await client.PostAsJsonAsync(
            "/api/v1/api-keys",
            new { name = "replacement", scope = "inventory.read", expiresInDays = (int?)null },
            JsonOptions);

        replacement.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    [InlineData(-1)]
    public async Task CreateKey_RejectsAnExpiryOutsideOneToThreeSixtyFiveDays(int days)
    {
        var client = await SignedInAdminAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/api-keys",
            new { name = "bad-expiry", scope = "inventory.read", expiresInDays = days },
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateKey_RejectsAnUnknownScope()
    {
        // A typo must not silently fall back to a scope the caller did not ask for.
        var client = await SignedInAdminAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/api-keys",
            new { name = "bad-scope", scope = "inventory.admin", expiresInDays = (int?)null },
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateKey_RejectsADuplicateNameForTheSamePrincipal()
    {
        var client = await SignedInAdminAsync();
        await CreateKeyAsync(client, name: "Home Assistant");

        var duplicate = await client.PostAsJsonAsync(
            "/api/v1/api-keys",
            new { name = "home assistant", scope = "inventory.read", expiresInDays = (int?)null },
            JsonOptions);

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict, "names are compared case-insensitively");
    }

    [Fact]
    public async Task CreateKey_TreatsUnderscoreInANameAsALiteral()
    {
        // Guards the LIKE-wildcard bug: with LIKE, "my_key" would collide with "myXkey".
        var client = await SignedInAdminAsync();
        await CreateKeyAsync(client, name: "my_key");

        var response = await client.PostAsJsonAsync(
            "/api/v1/api-keys",
            new { name = "myXkey", scope = "inventory.read", expiresInDays = (int?)null },
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateKey_WritesAnAuditEventWithoutTheSecret()
    {
        var client = await SignedInAdminAsync();
        var created = await CreateKeyAsync(client);
        var secret = created.Key[(created.Selector.Length + 1)..];

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var events = await db.AuditEvents.Where(e => e.EntityType == "ApiKey").ToListAsync();

        events.Should().ContainSingle(e => e.Action == AuditAction.Created);
        events.Should().NotContain(e => e.AfterPayload.Contains(secret), "the audit trail must never carry key material");
        events.Single(e => e.Action == AuditAction.Created).AfterPayload.Should().Contain(created.Selector);
    }

    [Fact]
    public async Task RevokeKey_WritesAnAuditEvent()
    {
        var client = await SignedInAdminAsync();
        var created = await CreateKeyAsync(client);

        await client.DeleteAsync($"/api/v1/api-keys/{created.Id}");

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var events = await db.AuditEvents.Where(e => e.EntityType == "ApiKey").ToListAsync();

        events.Should().Contain(e => e.Action == AuditAction.Deleted);
    }

    [Fact]
    public async Task WriteScopedKey_CanCreateADeviceAndReadScopedKeyCannot()
    {
        var client = await SignedInAdminAsync();
        var readKey = await CreateKeyAsync(client, name: "reader", scope: "inventory.read");
        var writeKey = await CreateKeyAsync(client, name: "writer", scope: "inventory.write");

        var readClient = factory.CreateClient();
        UseApiKey(readClient, readKey.Key);
        var writeClient = factory.CreateClient();
        UseApiKey(writeClient, writeKey.Key);

        // Both may read.
        (await readClient.GetAsync("/api/v1/devices")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await writeClient.GetAsync("/api/v1/devices")).StatusCode.Should().Be(HttpStatusCode.OK);

        // Only the write-scoped key may attempt a mutation. The read key is stopped by
        // authorization before validation, so it must be 403 and never 400.
        var readAttempt = await readClient.PostAsJsonAsync("/api/v1/devices", new { name = "x" }, JsonOptions);
        readAttempt.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var writeAttempt = await writeClient.PostAsJsonAsync("/api/v1/devices", new { name = "x" }, JsonOptions);
        writeAttempt.StatusCode.Should().NotBe(HttpStatusCode.Forbidden, "the write scope permits the attempt, whatever validation then says");
    }
}
