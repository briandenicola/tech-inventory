using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TechInventory.Domain.Enums;
using static TechInventory.IntegrationTests.ApiKeys.ApiKeyTestSupport;

namespace TechInventory.IntegrationTests.ApiKeys;

/// <summary>
/// N-21 — per-selector rate limiting.
/// </summary>
/// <remarks>
/// Uses a test-scoped limit of <see cref="ApiKeyTestHostFactory.TestPermitLimit"/>
/// requests per window rather than the production 60/min. Observing a 429 against the
/// production default would mean firing sixty requests per assertion: slow, and prone
/// to flaking under CI load. The ADR requires this override explicitly.
/// </remarks>
public sealed class ApiKeyRateLimitTests(ApiKeyTestHostFactory factory)
    : IClassFixture<ApiKeyTestHostFactory>
{
    private const string AdminUser = "ratelimit-admin";
    private const string Password = "Str0ng!TestPassword";

    private async Task<CreatedKeyDto> SetUpKeyAsync(string name)
    {
        await ResetDatabaseAsync(factory);
        await EnsureHouseholdAsync(factory);
        await ResetAndSeedLocalUserAsync(factory, OwnerRole.Admin, AdminUser, Password);

        var client = factory.CreateClient();
        UseBearer(client, await LoginAsync(client, AdminUser, Password));

        var response = await client.PostAsJsonAsync(
            "/api/v1/api-keys",
            new { name, scope = "inventory.write", expiresInDays = (int?)null },
            JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<CreatedKeyDto>(JsonOptions))!;
    }

    [Fact]
    public async Task N21_ExceedingThePerSelectorLimit_Returns429WithRetryAfter()
    {
        var created = await SetUpKeyAsync("rate-limited");
        var client = factory.CreateClient();
        UseApiKey(client, created.Key);

        HttpResponseMessage? limited = null;
        for (var attempt = 0; attempt < ApiKeyTestHostFactory.TestPermitLimit + 2; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/devices", new { name = "x" }, JsonOptions);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                limited = response;
                break;
            }
        }

        limited.Should().NotBeNull("the limiter must reject once the window's permits are spent");
        limited!.Headers.Should().ContainKey("Retry-After");
        (await limited.Content.ReadAsStringAsync()).Should().Contain("RateLimitExceeded");
    }

    [Fact]
    public async Task RateLimiting_IsPerSelectorNotGlobal()
    {
        // One noisy integration must not exhaust the budget of the owner's other keys.
        await ResetDatabaseAsync(factory);
        await EnsureHouseholdAsync(factory);
        await ResetAndSeedLocalUserAsync(factory, OwnerRole.Admin, AdminUser, Password);

        var bearer = factory.CreateClient();
        UseBearer(bearer, await LoginAsync(bearer, AdminUser, Password));

        async Task<CreatedKeyDto> MakeKey(string name)
        {
            var response = await bearer.PostAsJsonAsync(
                "/api/v1/api-keys",
                new { name, scope = "inventory.write", expiresInDays = (int?)null },
                JsonOptions);
            response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<CreatedKeyDto>(JsonOptions))!;
        }

        var noisy = await MakeKey("noisy");
        var quiet = await MakeKey("quiet");

        var noisyClient = factory.CreateClient();
        UseApiKey(noisyClient, noisy.Key);
        for (var attempt = 0; attempt < ApiKeyTestHostFactory.TestPermitLimit + 2; attempt++)
        {
            await noisyClient.PostAsJsonAsync("/api/v1/devices", new { name = "x" }, JsonOptions);
        }

        var quietClient = factory.CreateClient();
        UseApiKey(quietClient, quiet.Key);
        var quietResponse = await quietClient.GetAsync("/api/v1/devices");

        quietResponse.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
            "a second key's budget is independent of the first's");
    }
}
