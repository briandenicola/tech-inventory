using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TechInventory.Application.Abstractions.Services;
using TechInventory.IntegrationTests.Controllers;

namespace TechInventory.IntegrationTests.Auth;

public sealed class AuthIntegrationTests(IntegrationTestFactory<AuthIntegrationTests> factory)
    : ControllerTestBase<AuthIntegrationTests>(factory), IClassFixture<IntegrationTestFactory<AuthIntegrationTests>>
{
    [Fact]
    public async Task DevBypassEnabled_UnauthenticatedRequest_ReturnsSuccessWithDevAdmin()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/brands");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await WithDbContextAsync(async dbContext =>
        {
            var postResponse = await client.PostAsync("/api/v1/brands", CreateJsonContent(new
            {
                name = $"Brand-{Guid.NewGuid():N}",
                website = "https://example.com"
            }));

            postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var auditEvents = await dbContext.AuditEvents.ToListAsync();
            auditEvents.Should().ContainSingle();
            auditEvents[0].Actor.Should().Be("11111111-1111-1111-1111-111111111111");
        });
    }

    [Fact]
    public async Task DevBypassDisabled_NoToken_Returns401Unauthorized()
    {
        await ResetDatabaseAsync();
        await using var noAuthFactory = new NoAuthFactory();
        using var client = noAuthFactory.CreateClient();

        var response = await client.GetAsync("/api/v1/brands");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DevBypassDisabled_InvalidToken_Returns401Unauthorized()
    {
        await ResetDatabaseAsync();
        await using var noAuthFactory = new NoAuthFactory();
        using var client = noAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "garbage-token-value");

        var response = await client.GetAsync("/api/v1/brands");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DevBypassDisabled_ValidTokenWithAdminRole_ReturnsSuccess()
    {
        await ResetDatabaseAsync();
        await using var jwtFactory = new JwtAuthFactory();
        using var client = jwtFactory.CreateClient();

        var token = new TestJwtBuilder(jwtFactory.SigningKey, jwtFactory.Issuer, jwtFactory.Audience)
            .WithOid("22222222-2222-2222-2222-222222222222")
            .WithSubject("test-user@example.com")
            .WithName("Test Admin User")
            .WithEmail("test-user@example.com")
            .WithRoles("Admin")
            .Build();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/brands");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DevBypassDisabled_ValidTokenWithAdminRole_AuditLogsShowCorrectUser()
    {
        await ResetDatabaseAsync();
        await using var jwtFactory = new JwtAuthFactory();
        await jwtFactory.ResetStateAsync();
        using var client = jwtFactory.CreateClient();

        var oid = "33333333-3333-3333-3333-333333333333";
        var token = new TestJwtBuilder(jwtFactory.SigningKey, jwtFactory.Issuer, jwtFactory.Audience)
            .WithOid(oid)
            .WithSubject("audit-test@example.com")
            .WithName("Audit Test User")
            .WithEmail("audit-test@example.com")
            .WithRoles("Admin")
            .Build();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsync("/api/v1/brands", CreateJsonContent(new
        {
            name = $"Brand-{Guid.NewGuid():N}",
            website = "https://example.com"
        }));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        await jwtFactory.WithDbContextAsync(async dbContext =>
        {
            var auditEvents = await dbContext.AuditEvents
                .ToListAsync();

            var recentEvent = auditEvents
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefault();

            recentEvent.Should().NotBeNull();
            recentEvent!.Actor.Should().Be(oid);
        });
    }

    [Fact]
    public async Task DevBypassDisabled_ViewerRoleOnAdminEndpoint_Returns403Forbidden()
    {
        await ResetDatabaseAsync();
        await using var jwtFactory = new JwtAuthFactory();
        using var client = jwtFactory.CreateClient();

        var token = new TestJwtBuilder(jwtFactory.SigningKey, jwtFactory.Issuer, jwtFactory.Audience)
            .WithOid("44444444-4444-4444-4444-444444444444")
            .WithSubject("viewer@example.com")
            .WithName("Viewer User")
            .WithEmail("viewer@example.com")
            .WithRoles("Viewer")
            .Build();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var deleteResponse = await client.DeleteAsync($"/api/v1/brands/{Guid.NewGuid()}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DevBypassDisabled_TokenWithNoRoles_Returns401Unauthorized()
    {
        await ResetDatabaseAsync();
        await using var jwtFactory = new JwtAuthFactory();
        using var client = jwtFactory.CreateClient();

        var token = new TestJwtBuilder(jwtFactory.SigningKey, jwtFactory.Issuer, jwtFactory.Audience)
            .WithOid("55555555-5555-5555-5555-555555555555")
            .WithSubject("noroles@example.com")
            .WithName("No Roles User")
            .WithEmail("noroles@example.com")
            .Build();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/brands");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HttpContextCurrentUserService_ResolvesFromJwt()
    {
        await using var jwtFactory = new JwtAuthFactory();

        var oid = "66666666-6666-6666-6666-666666666666";
        var token = new TestJwtBuilder(jwtFactory.SigningKey, jwtFactory.Issuer, jwtFactory.Audience)
            .WithOid(oid)
            .WithSubject("service-test@example.com")
            .WithName("Service Test User")
            .WithEmail("service-test@example.com")
            .WithRoles("Member")
            .Build();

        using var scope = jwtFactory.Services.CreateScope();
        var currentUserService = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();

        using var client = jwtFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync("/api/v1/brands", CreateJsonContent(new
        {
            name = $"Brand-{Guid.NewGuid():N}",
            website = "https://example.com"
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await jwtFactory.WithDbContextAsync(async dbContext =>
        {
            var auditEvents = await dbContext.AuditEvents.ToListAsync();
            var auditEvent = auditEvents.OrderByDescending(e => e.Timestamp).FirstOrDefault();
            auditEvent.Should().NotBeNull();
            auditEvent!.Actor.Should().Be(oid);
        });
    }

    [Fact]
    public void ProductionWithDevBypass_ThrowsOnStartup()
    {
        try
        {
            using var _ = new ProductionDevBypassFactory();
            Assert.Fail("Expected InvalidOperationException but no exception was thrown");
        }
        catch (InvalidOperationException ex)
        {
            ex.Message.Should().Be("Auth:DevBypass may only be enabled in Development.");
        }
    }

    private sealed class NoAuthFactory : IntegrationTestFactory<NoAuthFactory>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:DevBypass"] = "false",
                    ["Auth:Entra:Authority"] = "https://login.microsoftonline.com/test-tenant/v2.0",
                    ["Auth:Entra:TenantId"] = "test-tenant-id",
                    ["Auth:Entra:ClientId"] = "test-client-id",
                    ["Auth:Entra:Audiences:0"] = "api://test-client-id",
                    ["Auth:Entra:Audiences:1"] = "test-client-id"
                }!);
            });
        }
    }

    private sealed class JwtAuthFactory : IntegrationTestFactory<JwtAuthFactory>
    {
        public RsaSecurityKey SigningKey { get; } = TestJwtBuilder.CreateTestSigningKey();
        public string Issuer => "https://login.microsoftonline.com/test-tenant-id/v2.0";
        public string Audience => "api://test-client-id";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:DevBypass"] = "false",
                    ["Auth:Entra:Authority"] = Issuer,
                    ["Auth:Entra:TenantId"] = "test-tenant-id",
                    ["Auth:Entra:ClientId"] = "test-client-id",
                    ["Auth:Entra:Audiences:0"] = Audience,
                    ["Auth:Entra:Audiences:1"] = "test-client-id"
                }!);
            });

            builder.ConfigureServices(services =>
            {
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Issuer,
                        ValidAudiences = new[] { Audience, "test-client-id" },
                        IssuerSigningKey = SigningKey,
                        ClockSkew = TimeSpan.FromMinutes(2)
                    };

                    options.Configuration = null!;
                    options.MetadataAddress = null!;
                });
            });
        }

        public async Task ResetStateAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TechInventory.Infrastructure.Persistence.AppDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.MigrateAsync();
        }

        public async Task<TResult> WithDbContextAsync<TResult>(Func<TechInventory.Infrastructure.Persistence.AppDbContext, Task<TResult>> action)
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TechInventory.Infrastructure.Persistence.AppDbContext>();
            return await action(dbContext);
        }

        public async Task WithDbContextAsync(Func<TechInventory.Infrastructure.Persistence.AppDbContext, Task> action)
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TechInventory.Infrastructure.Persistence.AppDbContext>();
            await action(dbContext);
        }
    }

    private sealed class ProductionDevBypassFactory : IntegrationTestFactory<ProductionDevBypassFactory>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");

            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:DevBypass"] = "true"
                }!);
            });

            base.ConfigureWebHost(builder);
        }
    }
}
