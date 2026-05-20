using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Settings;
using TechInventory.Domain.Entities;
using TechInventory.Domain.ValueObjects;

namespace TechInventory.IntegrationTests.Controllers;

public sealed class SettingsControllerTests(IntegrationTestFactory<SettingsControllerTests> factory)
    : ControllerTestBase<SettingsControllerTests>(factory), IClassFixture<IntegrationTestFactory<SettingsControllerTests>>
{
    [Fact]
    public async Task GetDisplaySettings_WhenNoSettingsExist_ReturnsDefaultsAndSeedsRows()
    {
        await ResetDatabaseAsync();
        await SeedAsync(entities: [new Household(Guid.NewGuid(), "Primary Household", Currency.From("USD"))]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/settings/display");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<DisplaySettingsResponse>(response);
        payload.Should().BeEquivalentTo(DisplaySettingsCatalog.GetDefaultResponse());
        var settingCount = await WithDbContextAsync(async dbContext => await dbContext.HouseholdSettings.CountAsync());
        settingCount.Should().Be(2);
    }

    [Fact]
    public async Task PutDisplaySettings_WhenValidRequest_UpdatesSettingsAndWritesAuditEvent()
    {
        await ResetDatabaseAsync();
        await SeedAsync(entities: [new Household(Guid.NewGuid(), "Primary Household", Currency.From("USD"))]);
        using var client = CreateClient();

        var response = await client.PutAsync("/api/v1/settings/display", CreateJsonContent(new
        {
            deviceListColumns = new[] { "name", "status", "brand", "category" },
            deviceDetailFields = new[] { "brand", "status", "notes", "owner" }
        }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<DisplaySettingsResponse>(response);
        payload.DeviceListColumns.Should().Equal("name", "status", "brand", "category");
        payload.DeviceDetailFields.Should().Equal("brand", "status", "notes", "owner");

        var reloadResponse = await client.GetAsync("/api/v1/settings/display");
        reloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reloaded = await ReadJsonAsync<DisplaySettingsResponse>(reloadResponse);
        reloaded.Should().BeEquivalentTo(payload);

        var auditCount = await WithDbContextAsync(async dbContext => await dbContext.AuditEvents.CountAsync(eventRow => eventRow.EntityType == nameof(HouseholdSetting)));
        auditCount.Should().Be(1);
    }

    [Fact]
    public async Task PutDisplaySettings_WhenRequestInvalid_Returns400ValidationProblemDetails()
    {
        await ResetDatabaseAsync();
        await SeedAsync(entities: [new Household(Guid.NewGuid(), "Primary Household", Currency.From("USD"))]);
        using var client = CreateClient();

        var response = await client.PutAsync("/api/v1/settings/display", CreateJsonContent(new
        {
            deviceListColumns = new[] { "brand", "status" },
            deviceDetailFields = new[] { "brand", "notes", "notes" }
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ReadValidationProblemDetailsAsync(response);
        problem.Errors.Keys.Should().Contain(key => string.Equals(key, "DeviceListColumns", StringComparison.OrdinalIgnoreCase));
        problem.Errors.Keys.Should().Contain(key => string.Equals(key, "DeviceDetailFields", StringComparison.OrdinalIgnoreCase));
    }
}
