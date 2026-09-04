using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechInventory.Application.Abstractions.Services;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.Domain.ValueObjects;
using TechInventory.Infrastructure.Persistence;

namespace TechInventory.IntegrationTests.ApiKeys;

/// <summary>
/// Host for the API key suite (#149).
/// </summary>
/// <remarks>
/// <para>
/// <c>UseTestAuth = false</c> is essential: the shared factory replaces the whole
/// authentication stack with <c>TestAuthHandler</c>, which would bypass the very
/// pipeline these tests exist to exercise. Bearer callers here hold real
/// locally-issued HS256 tokens, so the bearer-regression cases (N-19/N-20) prove
/// something about the real routing rather than about a stub.
/// </para>
/// <para>
/// Rate limits are dialled down to a handful of requests per window. Exercising the
/// production 60/min default would mean firing 60 requests to observe one 429 —
/// slow, and flaky under CI load (ADR 0003, N-21).
/// </para>
/// </remarks>
public class ApiKeyTestHostFactory : IntegrationTestFactory<ApiKeyTestHostFactory>
{
    /// <summary>Small enough that a 429 is reached in a few requests, not sixty.</summary>
    public const int TestPermitLimit = 3;

    public const int TestWindowSeconds = 60;

    protected override string Environment => "Testing";

    protected override bool UseTestAuth => false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Entra:Authority"] = "https://login.microsoftonline.com/test-tenant/v2.0",
                ["Auth:Entra:Audiences:0"] = "api://test-client-id",
                ["Auth:Local:SigningKey"] = "integration-test-local-signing-key-32-bytes-long!",
                ["Auth:Local:Audience"] = "techinventory-api",
                ["Auth:Local:AccessTokenLifetimeMinutes"] = "60",
                ["Auth:Local:Argon2:MemoryKib"] = "1024",
                ["Auth:Local:Argon2:Iterations"] = "1",
                ["Auth:Local:Argon2:Parallelism"] = "1",
                ["RateLimiting:ApiKey:PermitLimit"] = TestPermitLimit.ToString(),
                ["RateLimiting:ApiKey:WindowSeconds"] = TestWindowSeconds.ToString(),
            });
        });
    }
}

/// <summary>A host whose rate limit is effectively disabled, for tests that are not about limits.</summary>
public class ApiKeyUnthrottledTestHostFactory : ApiKeyTestHostFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:ApiKey:PermitLimit"] = "10000",
            });
        });
    }
}

/// <summary>Shared setup helpers for the API key suite.</summary>
public static class ApiKeyTestSupport
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<LocalUser> ResetAndSeedLocalUserAsync<TFactory>(
        IntegrationTestFactory<TFactory> factory,
        OwnerRole role,
        string username,
        string password,
        bool isActive = true)
        where TFactory : class
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = new LocalUser(
            Guid.NewGuid(),
            username,
            displayName: $"{role} User",
            role: role,
            passwordHash: hasher.Hash(password),
            passwordAlgorithm: hasher.CurrentAlgorithm,
            mustChangePasswordOnNextLogin: false,
            createdBy: "test");

        if (!isActive)
        {
            user.Deactivate("test");
        }

        await db.LocalUsers.AddAsync(user);
        await db.SaveChangesAsync();
        return user;
    }

    public static async Task ResetDatabaseAsync<TFactory>(IntegrationTestFactory<TFactory> factory)
        where TFactory : class
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public static async Task EnsureHouseholdAsync<TFactory>(IntegrationTestFactory<TFactory> factory)
        where TFactory : class
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!await db.Households.AnyAsync())
        {
            await db.Households.AddAsync(new Household(Guid.NewGuid(), "Test Household", Currency.From("USD")));
            await db.SaveChangesAsync();
        }
    }

    /// <summary>Exchanges local credentials for a real HS256 bearer token.</summary>
    public static async Task<string> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/local/login", new { username, password });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        return payload!.AccessToken;
    }

    public static void UseBearer(HttpClient client, string token)
        => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public static void UseApiKey(HttpClient client, string key)
        => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", key);

    public sealed record LoginResponseDto(string AccessToken, long ExpiresInSeconds, bool MustChangePassword);

    public sealed record CreatedKeyDto(
        Guid Id,
        string Name,
        string Selector,
        string Scope,
        DateTimeOffset CreatedAt,
        DateTimeOffset ExpiresAt,
        string Key);

    public sealed record KeyDto(
        Guid Id,
        string Name,
        string Selector,
        string Scope,
        DateTimeOffset CreatedAt,
        DateTimeOffset ExpiresAt,
        DateTimeOffset? RevokedAt,
        bool IsActive,
        string? OwnerDisplayName);

    public sealed record PagedDto<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
}
