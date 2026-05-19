using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechInventory.Application.Abstractions.Services;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.Infrastructure.Persistence;

namespace TechInventory.IntegrationTests.Auth;

/// <summary>
/// F025 — end-to-end behaviour of /api/v1/auth/local/login + change-password.
/// Uses a dedicated factory that disables DevBypass and enables the local
/// signing key so the actual JWT scheme is exercised.
/// </summary>
public sealed class LocalAuthEndpointTests : IClassFixture<LocalAuthEndpointTests.LocalAuthFactory>
{
    private const string TestPassword = "Sup3rSecurePass!2026";
    private const string TestUsername = "breakglass";

    private readonly LocalAuthFactory _factory;

    public LocalAuthEndpointTests(LocalAuthFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        await SeedLocalUserAsync(mustChangePassword: false);
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/local/login", new { username = TestUsername, password = TestPassword });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.ExpiresInSeconds.Should().BeGreaterThan(0);
        body.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        await SeedLocalUserAsync(mustChangePassword: false);
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/local/login", new { username = TestUsername, password = "wrong-password" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownUsername_Returns401SameAsWrongPassword()
    {
        await SeedLocalUserAsync(mustChangePassword: false);
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/local/login", new { username = "nobody", password = "anything" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_BlocksLocalUserWhileMustChangePassword()
    {
        await SeedLocalUserAsync(mustChangePassword: true);
        using var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/local/login", new { username = TestUsername, password = TestPassword });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        login!.MustChangePassword.Should().BeTrue();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var brandsResponse = await client.GetAsync("/api/v1/brands");
        brandsResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangePassword_ValidFlow_AllowsSubsequentLoginsWithoutMustChange()
    {
        await SeedLocalUserAsync(mustChangePassword: true);
        using var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/local/login", new { username = TestUsername, password = TestPassword });
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);

        var newPassword = "EvenStr0nger!Pass2026";
        var changeResponse = await client.PostAsJsonAsync("/api/v1/auth/local/change-password", new { currentPassword = TestPassword, newPassword });
        changeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Sign in again with the new password and verify the flag is now clear.
        using var nextClient = _factory.CreateClient();
        var nextLogin = await nextClient.PostAsJsonAsync("/api/v1/auth/local/login", new { username = TestUsername, password = newPassword });
        nextLogin.StatusCode.Should().Be(HttpStatusCode.OK);
        var nextBody = await nextLogin.Content.ReadFromJsonAsync<LoginResponseDto>();
        nextBody!.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns401()
    {
        await SeedLocalUserAsync(mustChangePassword: true);
        using var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/local/login", new { username = TestUsername, password = TestPassword });
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);

        var changeResponse = await client.PostAsJsonAsync("/api/v1/auth/local/change-password", new { currentPassword = "not-the-current", newPassword = "NewlyN0t!ReallyValid" });
        changeResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task SeedLocalUserAsync(bool mustChangePassword)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var user = new LocalUser(
            Guid.NewGuid(),
            TestUsername,
            displayName: "Break Glass Admin",
            role: OwnerRole.Admin,
            passwordHash: hasher.Hash(TestPassword),
            passwordAlgorithm: hasher.CurrentAlgorithm,
            mustChangePasswordOnNextLogin: mustChangePassword,
            createdBy: "test");
        await db.LocalUsers.AddAsync(user);
        await db.SaveChangesAsync();
    }

    private sealed record LoginResponseDto(string AccessToken, long ExpiresInSeconds, bool MustChangePassword);

    public sealed class LocalAuthFactory : IntegrationTestFactory<LocalAuthFactory>
    {
        protected override string Environment => "Testing";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:DevBypass"] = "false",
                    // Entra wiring is required by the startup-config check even
                    // though we never exchange Entra tokens here.
                    ["Auth:Entra:Authority"] = "https://login.microsoftonline.com/test-tenant/v2.0",
                    ["Auth:Entra:Audiences:0"] = "api://test-client-id",
                    ["Auth:Local:SigningKey"] = "integration-test-local-signing-key-32-bytes-long!",
                    ["Auth:Local:Audience"] = "techinventory-api",
                    ["Auth:Local:AccessTokenLifetimeMinutes"] = "60",
                    ["Auth:Local:Argon2:MemoryKib"] = "1024",
                    ["Auth:Local:Argon2:Iterations"] = "1",
                    ["Auth:Local:Argon2:Parallelism"] = "1"
                });
            });
        }
    }
}
