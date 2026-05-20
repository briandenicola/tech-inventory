using FluentAssertions;
using TechInventory.Application.Settings;

namespace TechInventory.UnitTests.Application;

public sealed class DisplaySettingsValidationTests
{
    [Fact]
    public async Task UpdateDisplaySettingsCommand_WhenDeviceListColumnsContainDuplicates_ReturnsValidationFailure()
    {
        var command = new UpdateDisplaySettingsCommand(
            ["name", "brand", "Brand"],
            ["brand", "status", "notes"]);

        var result = await DeviceHandlerTestSupport.ValidateAsync(
            command,
            new UpdateDisplaySettingsCommandValidator(),
            DisplaySettingsCatalog.GetDefaultResponse());

        result.IsFailure.Should().BeTrue();
        result.Error!.ValidationErrors.Keys.Should().Contain(key => string.Equals(key, "DeviceListColumns", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpdateDisplaySettingsCommand_WhenDeviceListColumnsOmitName_ReturnsValidationFailure()
    {
        var command = new UpdateDisplaySettingsCommand(
            ["brand", "category", "status"],
            ["brand", "status", "notes"]);

        var result = await DeviceHandlerTestSupport.ValidateAsync(
            command,
            new UpdateDisplaySettingsCommandValidator(),
            DisplaySettingsCatalog.GetDefaultResponse());

        result.IsFailure.Should().BeTrue();
        result.Error!.ValidationErrors.Keys.Should().Contain(key => string.Equals(key, "DeviceListColumns", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpdateDisplaySettingsCommand_WhenDeviceDetailFieldsContainUnknownIdentifier_ReturnsValidationFailure()
    {
        var command = new UpdateDisplaySettingsCommand(
            ["name", "brand", "status"],
            ["brand", "launchDate", "notes"]);

        var result = await DeviceHandlerTestSupport.ValidateAsync(
            command,
            new UpdateDisplaySettingsCommandValidator(),
            DisplaySettingsCatalog.GetDefaultResponse());

        result.IsFailure.Should().BeTrue();
        result.Error!.ValidationErrors.Keys.Should().Contain(key => key.StartsWith("DeviceDetailFields", StringComparison.OrdinalIgnoreCase));
    }
}
