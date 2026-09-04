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
/// Spec 005 §6 — the negative and tamper scenarios N-1..N-18. Rate limiting (N-21)
/// lives in <see cref="ApiKeyRateLimitTests"/> because it needs a throttled host.
/// </summary>
/// <remarks>
/// Every rejection here must be the same 401 with the same <c>InvalidApiKey</c> code.
/// Asserting the code, not just the status, is the point: a future change that made
/// any one case more descriptive would turn the endpoint into an oracle for
/// enumerating valid selectors, and these tests are what would catch it.
/// </remarks>
[Collection("ApiKeyUnthrottled")]
public sealed class ApiKeyNegativeScenarioTests(ApiKeyUnthrottledTestHostFactory factory)
    : IClassFixture<ApiKeyUnthrottledTestHostFactory>
{
    private const string AdminUser = "neg-admin";
    private const string MemberUser = "neg-member";
    private const string ViewerUser = "neg-viewer";
    private const string Password = "Str0ng!TestPassword";

    private async Task PrepareAsync()
    {
        await ResetDatabaseAsync(factory);
        await EnsureHouseholdAsync(factory);
    }

    private async Task<HttpClient> SignInAsync(string username, OwnerRole role, bool seed = true)
    {
        if (seed)
        {
            await ResetAndSeedLocalUserAsync(factory, role, username, Password);
        }

        var client = factory.CreateClient();
        UseBearer(client, await LoginAsync(client, username, Password));
        return client;
    }

    private static async Task<CreatedKeyDto> CreateKeyAsync(HttpClient client, string name, string scope = "inventory.read")
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/api-keys",
            new { name, scope, expiresInDays = (int?)null },
            JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CreatedKeyDto>(JsonOptions))!;
    }

    private static async Task AssertInvalidApiKeyAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, body);
        body.Should().Contain("InvalidApiKey",
            "every API key failure mode must be indistinguishable from the others");
    }

    // ---------------------------------------------------------------- N-1

    [Fact]
    public async Task N01_UnknownSelector_Returns401InvalidApiKey()
    {
        await PrepareAsync();
        var client = factory.CreateClient();
        UseApiKey(client, "bm90LWEtcmVhbC1zZWxlY3Rvcg.bm90LWEtcmVhbC1zZWNyZXQ");

        await AssertInvalidApiKeyAsync(await client.GetAsync("/api/v1/devices"));
    }

    // ---------------------------------------------------------------- N-2

    [Fact]
    public async Task N02_CorrectSelectorWrongSecret_Returns401InvalidApiKey()
    {
        await PrepareAsync();
        var admin = await SignInAsync(AdminUser, OwnerRole.Admin);
        var created = await CreateKeyAsync(admin, "n02");

        var client = factory.CreateClient();
        UseApiKey(client, $"{created.Selector}.bm90LXRoZS1yaWdodC1zZWNyZXQ");

        await AssertInvalidApiKeyAsync(await client.GetAsync("/api/v1/devices"));
    }

    // ---------------------------------------------------------------- N-3

    [Fact]
    public async Task N03_ExpiredKey_Returns401InvalidApiKey()
    {
        await PrepareAsync();
        var admin = await SignInAsync(AdminUser, OwnerRole.Admin);
        var created = await CreateKeyAsync(admin, "n03");

        // Expiry is enforced against stored state, so ageing the row is a faithful
        // substitute for waiting 90 days and keeps the test deterministic.
        //
        // Written through the change tracker rather than ExecuteUpdateAsync: the bulk
        // path bypasses the provider's DateTimeOffset conversion and writes a value the
        // materialiser then cannot read back, which surfaces as a 500 instead of the
        // 401 under test.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var key = await db.ApiKeys.SingleAsync(k => k.Id == created.Id);
            db.Entry(key).Property(nameof(TechInventory.Domain.Entities.ApiKey.ExpiresAt)).CurrentValue =
                DateTimeOffset.UtcNow.AddDays(-1);
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        UseApiKey(client, created.Key);

        await AssertInvalidApiKeyAsync(await client.GetAsync("/api/v1/devices"));
    }

    // ---------------------------------------------------------------- N-4

    [Fact]
    public async Task N04_RevokedKey_Returns401InvalidApiKey()
    {
        await PrepareAsync();
        var admin = await SignInAsync(AdminUser, OwnerRole.Admin);
        var created = await CreateKeyAsync(admin, "n04");
        await admin.DeleteAsync($"/api/v1/api-keys/{created.Id}");

        var client = factory.CreateClient();
        UseApiKey(client, created.Key);

        await AssertInvalidApiKeyAsync(await client.GetAsync("/api/v1/devices"));
    }

    // ---------------------------------------------------------------- N-5

    [Fact]
    public async Task N05_DeactivatedPrincipal_Returns401InvalidApiKey()
    {
        await PrepareAsync();
        var admin = await SignInAsync(AdminUser, OwnerRole.Admin);
        var created = await CreateKeyAsync(admin, "n05");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.LocalUsers.SingleAsync(u => u.Username == AdminUser);
            user.Deactivate("test");
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        UseApiKey(client, created.Key);

        await AssertInvalidApiKeyAsync(await client.GetAsync("/api/v1/devices"));
    }

    // ---------------------------------------------------------------- N-6

    [Fact]
    public async Task N06_PrincipalDemotedToViewer_Returns401InvalidApiKey()
    {
        // The live ceiling: a key must not outlive its holder's right to have created it.
        await PrepareAsync();
        var admin = await SignInAsync(AdminUser, OwnerRole.Admin);
        var created = await CreateKeyAsync(admin, "n06");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.LocalUsers.SingleAsync(u => u.Username == AdminUser);
            user.SetRole(OwnerRole.Viewer, "test");
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        UseApiKey(client, created.Key);

        await AssertInvalidApiKeyAsync(await client.GetAsync("/api/v1/devices"));
    }

    // ---------------------------------------------------------------- N-7

    [Fact]
    public async Task N07_ReadScopedKeyPostingADevice_Returns403()
    {
        await PrepareAsync();
        var admin = await SignInAsync(AdminUser, OwnerRole.Admin);
        var created = await CreateKeyAsync(admin, "n07", "inventory.read");

        var client = factory.CreateClient();
        UseApiKey(client, created.Key);

        var response = await client.PostAsJsonAsync("/api/v1/devices", new { name = "nope" }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------------------------------------------------------------- N-8, N-9

    [Theory]
    [InlineData("GET", "/api/v1/audit-events")]
    [InlineData("GET", "/api/v1/reports/summary")]
    [InlineData("GET", "/api/v1/settings/display")]
    [InlineData("GET", "/api/v1/exports/devices")]
    public async Task N08_N09_WriteScopedKeyOnOutOfScopeEndpoints_Returns403(string method, string path)
    {
        // Even the broader scope reaches nothing outside inventory, and even for an
        // Admin principal — the ceiling is the scope, not the role.
        await PrepareAsync();
        var admin = await SignInAsync(AdminUser, OwnerRole.Admin);
        var created = await CreateKeyAsync(admin, "n08", "inventory.write");

        var client = factory.CreateClient();
        UseApiKey(client, created.Key);

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "{0} {1} is outside the inventory scope", method, path);
    }

    // ---------------------------------------------------------------- N-10

    [Fact]
    public async Task N10_ApiKeyCannotBootstrapMoreKeys_Returns403()
    {
        await PrepareAsync();
        var admin = await SignInAsync(AdminUser, OwnerRole.Admin);
        var created = await CreateKeyAsync(admin, "n10", "inventory.write");

        var client = factory.CreateClient();
        UseApiKey(client, created.Key);

        var create = await client.PostAsJsonAsync(
            "/api/v1/api-keys",
            new { name = "self-minted", scope = "inventory.write", expiresInDays = (int?)null },
            JsonOptions);
        create.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var list = await client.GetAsync("/api/v1/api-keys");
        list.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var revoke = await client.DeleteAsync($"/api/v1/api-keys/{created.Id}");
        revoke.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------------------------------------------------------------- N-11

    [Fact]
    public async Task N11_BothBearerAndApiKeyHeaders_Returns401AmbiguousCredential()
    {
        await PrepareAsync();
        var admin = await SignInAsync(AdminUser, OwnerRole.Admin);
        var created = await CreateKeyAsync(admin, "n11");
        var bearer = await LoginAsync(factory.CreateClient(), AdminUser, Password);

        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/devices");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {bearer}");
        request.Headers.TryAddWithoutValidation("Authorization", $"ApiKey {created.Key}");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().Contain("AmbiguousCredential");
    }

    [Fact]
    public async Task N11_CommaJoinedCredentials_AreAlsoRejected()
    {
        // The other shape the same attack takes: one header, two credentials.
        await PrepareAsync();
        var admin = await SignInAsync(AdminUser, OwnerRole.Admin);
        var created = await CreateKeyAsync(admin, "n11b");
        var bearer = await LoginAsync(factory.CreateClient(), AdminUser, Password);

        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/devices");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {bearer}, ApiKey {created.Key}");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().Contain("AmbiguousCredential");
    }

    // ---------------------------------------------------------------- N-12

    [Theory]
    [InlineData("malformed-no-dot")]
    [InlineData(".secret-only")]
    [InlineData("selector-only.")]
    [InlineData("")]
    public async Task N12_MalformedCredential_Returns401InvalidApiKey(string credential)
    {
        await PrepareAsync();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"ApiKey {credential}");

        var response = await client.GetAsync("/api/v1/devices");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------------------------------------------------------------- N-13

    [Fact]
    public async Task N13_ViewerCannotCreateAKey_Returns403()
    {
        await PrepareAsync();
        var viewer = await SignInAsync(ViewerUser, OwnerRole.Viewer);

        var response = await viewer.PostAsJsonAsync(
            "/api/v1/api-keys",
            new { name = "viewer-key", scope = "inventory.read", expiresInDays = (int?)null },
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------------------------------------------------------------- N-14

    [Fact]
    public async Task N14_MemberCannotRevokeAnotherMembersKey_Returns403()
    {
        await PrepareAsync();
        var admin = await SignInAsync(AdminUser, OwnerRole.Admin);
        var adminKey = await CreateKeyAsync(admin, "admin-key");

        await ResetAndSeedLocalUserAsync(factory, OwnerRole.Member, MemberUser, Password);
        var member = factory.CreateClient();
        UseBearer(member, await LoginAsync(member, MemberUser, Password));

        var response = await member.DeleteAsync($"/api/v1/api-keys/{adminKey.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminCanRevokeAnotherPrincipalsKey()
    {
        // US-4, the counterpart to N-14: emergency revocation must cross ownership.
        await PrepareAsync();
        await ResetAndSeedLocalUserAsync(factory, OwnerRole.Member, MemberUser, Password);
        var member = factory.CreateClient();
        UseBearer(member, await LoginAsync(member, MemberUser, Password));
        var memberKey = await CreateKeyAsync(member, "member-key");

        var admin = await SignInAsync(AdminUser, OwnerRole.Admin);

        var response = await admin.DeleteAsync($"/api/v1/api-keys/{memberKey.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MemberListingKeys_SeesOnlyTheirOwn()
    {
        await PrepareAsync();
        var admin = await SignInAsync(AdminUser, OwnerRole.Admin);
        await CreateKeyAsync(admin, "admin-only-key");

        await ResetAndSeedLocalUserAsync(factory, OwnerRole.Member, MemberUser, Password);
        var member = factory.CreateClient();
        UseBearer(member, await LoginAsync(member, MemberUser, Password));
        await CreateKeyAsync(member, "member-own-key");

        var listed = await member.GetFromJsonAsync<PagedDto<KeyDto>>("/api/v1/api-keys", JsonOptions);

        listed!.Items.Should().ContainSingle();
        listed.Items[0].Name.Should().Be("member-own-key");
    }

    // ---------------------------------------------------------------- N-16

    [Fact]
    public async Task N16_TheSecretNeverAppearsInAnyResponseBodyAfterCreation()
    {
        await PrepareAsync();
        var admin = await SignInAsync(AdminUser, OwnerRole.Admin);
        var created = await CreateKeyAsync(admin, "n16");
        var secret = created.Key[(created.Selector.Length + 1)..];

        var list = await (await admin.GetAsync("/api/v1/api-keys")).Content.ReadAsStringAsync();
        var all = await (await admin.GetAsync("/api/v1/api-keys/all")).Content.ReadAsStringAsync();

        list.Should().NotContain(secret);
        all.Should().NotContain(secret);
    }

    // ---------------------------------------------------------------- N-19, N-20

    [Fact]
    public async Task N19_N20_BearerRoutingIsUnchangedByTheApiKeyBranch()
    {
        // Regression guard for the new forwarding branch: an ordinary local bearer must
        // still authenticate and carry the same role claims it always did.
        await PrepareAsync();
        var admin = await SignInAsync(AdminUser, OwnerRole.Admin);

        (await admin.GetAsync("/api/v1/devices")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.GetAsync("/api/v1/audit-events")).StatusCode.Should().Be(HttpStatusCode.OK,
            "a bearer Admin still reaches endpoints that API keys cannot");
        (await admin.GetAsync("/api/v1/api-keys")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnauthenticatedRequest_IsStillRejected()
    {
        await PrepareAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/devices");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
