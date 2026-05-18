using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TechInventory.Domain.Entities;

namespace TechInventory.IntegrationTests.Controllers;

public sealed class DevAuthBypassTests(IntegrationTestFactory<DevAuthBypassTests> factory)
    : ControllerTestBase<DevAuthBypassTests>(factory), IClassFixture<IntegrationTestFactory<DevAuthBypassTests>>
{
    private const string DevBypassUserId = "11111111-1111-1111-1111-111111111111";

    [Fact]
    public async Task ProtectedEndpoint_WhenCalledWithoutRealToken_UsesAdminBypassIdentity()
    {
        await ResetDatabaseAsync();
        await using var probeFactory = new AdminRoleProbeFactory();
        await probeFactory.ResetStateAsync();
        using var client = probeFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var protectedResponse = await client.GetAsync("/api/v1/devices");
        var adminRoleResponse = await client.GetAsync("/__test/admin-role");

        protectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        adminRoleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Mutations_WhenBypassIdentityIsActive_RecordStableAdminActor()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var firstCreate = await client.PostAsync("/api/v1/brands", CreateJsonContent(new
        {
            name = $"Brand-{Guid.NewGuid():N}",
            website = "https://example.com",
            notes = "created under bypass"
        }));

        var secondCreate = await client.PostAsync("/api/v1/networks", CreateJsonContent(new
        {
            name = $"Network-{Guid.NewGuid():N}",
            description = "created under bypass"
        }));

        firstCreate.StatusCode.Should().Be(HttpStatusCode.Created);
        secondCreate.StatusCode.Should().Be(HttpStatusCode.Created);

        await WithDbContextAsync(async dbContext =>
        {
            var actors = (await dbContext.AuditEvents
                .Select(auditEvent => new { auditEvent.Actor, auditEvent.Timestamp })
                .ToListAsync())
                .OrderByDescending(auditEvent => auditEvent.Timestamp)
                .Select(auditEvent => auditEvent.Actor)
                .Take(2)
                .ToArray();

            actors.Should().HaveCount(2);
            actors.Should().OnlyContain(actor => actor == DevBypassUserId);
            actors.Distinct(StringComparer.Ordinal).Should().ContainSingle();
        });
    }

    private sealed class AdminRoleProbeFactory : IntegrationTestFactory<AdminRoleProbeFactory>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, AdminRoleProbeStartupFilter>());
        }

        public async Task ResetStateAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TechInventory.Infrastructure.Persistence.AppDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.MigrateAsync();
        }

        private sealed class AdminRoleProbeStartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
                => app =>
                {
                    next(app);
                    app.Map("/__test/admin-role", branch =>
                    {
                        branch.Run(async context =>
                        {
                            context.Response.StatusCode = context.User.IsInRole("Admin")
                                ? StatusCodes.Status200OK
                                : StatusCodes.Status403Forbidden;
                            await context.Response.CompleteAsync();
                        });
                    });
                };
        }
    }
}
