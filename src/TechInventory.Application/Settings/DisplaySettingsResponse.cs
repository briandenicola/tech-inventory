namespace TechInventory.Application.Settings;

public sealed record DisplaySettingsResponse(
    IReadOnlyList<string> DeviceListColumns,
    IReadOnlyList<string> DeviceDetailFields);
