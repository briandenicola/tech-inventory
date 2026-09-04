using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TechInventory.Application.Abstractions.Services;
using TechInventory.Domain.Enums;
using static TechInventory.IntegrationTests.ApiKeys.ApiKeyTestSupport;

namespace TechInventory.IntegrationTests.ApiKeys;

/// <summary>
/// N-18 — the unknown-selector path must perform the same cryptographic work as a
/// real verification, so "this selector exists" is not observable.
/// </summary>
/// <remarks>
/// <para>
/// Asserted by <b>counting calls</b>, not by measuring time. ADR 0003 forbids any
/// wall-clock or "&lt;1ms variance" assertion here: such a test proves nothing on a
/// shared CI runner and fails randomly under load. What actually matters is a
/// structural property — that no early <c>return</c> skips the dummy comparison — and
/// a counting decorator establishes that deterministically.
/// </para>
/// </remarks>
public sealed class ApiKeyDummyVerifierPathTests(ApiKeyDummyPathFactory factory)
    : IClassFixture<ApiKeyDummyPathFactory>
{
    private const string AdminUser = "dummy-path-admin";
    private const string Password = "Str0ng!TestPassword";

    [Fact]
    public async Task UnknownSelector_StillRunsAFixedTimeComparison()
    {
        await ResetDatabaseAsync(factory);
        await EnsureHouseholdAsync(factory);
        factory.Counter.Reset();

        var client = factory.CreateClient();
        UseApiKey(client, "dW5rbm93bi1zZWxlY3Rvcg.dW5rbm93bi1zZWNyZXQ");

        var response = await client.GetAsync("/api/v1/devices");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        factory.Counter.DummyVerifications.Should().Be(1,
            "an unknown selector must still perform one dummy comparison before failing");
        factory.Counter.RealVerifications.Should().Be(0,
            "there is no stored verifier to compare against");
    }

    [Fact]
    public async Task KnownSelector_RunsTheRealComparison()
    {
        await ResetDatabaseAsync(factory);
        await EnsureHouseholdAsync(factory);
        await ResetAndSeedLocalUserAsync(factory, OwnerRole.Admin, AdminUser, Password);

        var bearer = factory.CreateClient();
        UseBearer(bearer, await LoginAsync(bearer, AdminUser, Password));
        var createResponse = await bearer.PostAsJsonAsync(
            "/api/v1/api-keys",
            new { name = "dummy-path", scope = "inventory.read", expiresInDays = (int?)null },
            JsonOptions);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, await createResponse.Content.ReadAsStringAsync());
        var created = (await createResponse.Content.ReadFromJsonAsync<CreatedKeyDto>(JsonOptions))!;

        factory.Counter.Reset();

        var client = factory.CreateClient();
        UseApiKey(client, created.Key);
        var response = await client.GetAsync("/api/v1/devices");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.Counter.RealVerifications.Should().Be(1);
    }

    [Fact]
    public async Task WrongSecretForAKnownSelector_RunsTheRealComparisonNotTheDummy()
    {
        // The two failure branches must be told apart only by which comparison ran —
        // never by the response, which is identical.
        await ResetDatabaseAsync(factory);
        await EnsureHouseholdAsync(factory);
        await ResetAndSeedLocalUserAsync(factory, OwnerRole.Admin, AdminUser, Password);

        var bearer = factory.CreateClient();
        UseBearer(bearer, await LoginAsync(bearer, AdminUser, Password));
        var createResponse = await bearer.PostAsJsonAsync(
            "/api/v1/api-keys",
            new { name = "wrong-secret", scope = "inventory.read", expiresInDays = (int?)null },
            JsonOptions);
        var created = (await createResponse.Content.ReadFromJsonAsync<CreatedKeyDto>(JsonOptions))!;

        factory.Counter.Reset();

        var client = factory.CreateClient();
        UseApiKey(client, $"{created.Selector}.d3Jvbmctc2VjcmV0");
        var response = await client.GetAsync("/api/v1/devices");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        factory.Counter.RealVerifications.Should().Be(1);
    }

    [Fact]
    public async Task MalformedCredential_StillRunsADummyComparison()
    {
        await ResetDatabaseAsync(factory);
        await EnsureHouseholdAsync(factory);
        factory.Counter.Reset();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "ApiKey no-separator-here");

        var response = await client.GetAsync("/api/v1/devices");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        factory.Counter.DummyVerifications.Should().Be(1,
            "a malformed credential must cost the same as a well-formed miss");
    }
}

/// <summary>Host that wraps <see cref="IApiKeyHasher"/> in a counting decorator.</summary>
public sealed class ApiKeyDummyPathFactory : ApiKeyUnthrottledTestHostFactory
{
    public VerificationCounter Counter { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            // Decorate rather than replace: the real hasher still does the real work, so
            // this observes the production code path instead of standing in for it.
            var descriptor = services.Single(service => service.ServiceType == typeof(IApiKeyHasher));
            services.Remove(descriptor);
            services.AddSingleton<IApiKeyHasher>(provider =>
                new CountingApiKeyHasher(
                    ActivatorUtilities.CreateInstance<Infrastructure.Services.HmacApiKeyHasher>(provider),
                    Counter));
        });
    }
}

public sealed class VerificationCounter
{
    private int _real;
    private int _dummy;

    public int RealVerifications => Volatile.Read(ref _real);

    public int DummyVerifications => Volatile.Read(ref _dummy);

    public void CountReal() => Interlocked.Increment(ref _real);

    public void CountDummy() => Interlocked.Increment(ref _dummy);

    public void Reset()
    {
        Volatile.Write(ref _real, 0);
        Volatile.Write(ref _dummy, 0);
    }
}

public sealed class CountingApiKeyHasher(IApiKeyHasher inner, VerificationCounter counter) : IApiKeyHasher
{
    public string ComputeVerifier(string secret) => inner.ComputeVerifier(secret);

    public bool VerifyConstantTime(string presentedSecret, string expectedVerifier)
    {
        counter.CountReal();
        return inner.VerifyConstantTime(presentedSecret, expectedVerifier);
    }

    public bool VerifyDummyConstantTime(string presentedSecret)
    {
        counter.CountDummy();
        return inner.VerifyDummyConstantTime(presentedSecret);
    }
}
