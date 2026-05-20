using System.Net;
using FluentAssertions;
using TechInventory.IntegrationTests.Support;
using TechInventory.Domain.Entities;
using TechInventory.Domain.ValueObjects;

namespace TechInventory.IntegrationTests.Controllers;

public sealed class SettingsAuthorizationTests(MemberRoleIntegrationTestFactory<SettingsAuthorizationTests> factory)
    : ControllerTestBase<SettingsAuthorizationTests>(factory), IClassFixture<MemberRoleIntegrationTestFactory<SettingsAuthorizationTests>>
{
    [Fact]
    public async Task GetDisplaySettings_WhenCallerIsMember_ReturnsOk()
    {
        await ResetDatabaseAsync();
        await SeedAsync(entities: [new Household(Guid.NewGuid(), "Primary Household", Currency.From("USD"))]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/settings/display");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PutDisplaySettings_WhenCallerIsMember_ReturnsForbidden()
    {
        await ResetDatabaseAsync();
        await SeedAsync(entities: [new Household(Guid.NewGuid(), "Primary Household", Currency.From("USD"))]);
        using var client = CreateClient();

        var response = await client.PutAsync("/api/v1/settings/display", CreateJsonContent(new
        {
            deviceListColumns = new[] { "name", "status", "brand" },
            deviceDetailFields = new[] { "brand", "status", "notes" }
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
