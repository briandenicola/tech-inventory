using TechInventory.Domain.Entities;

namespace TechInventory.Application.Settings;

public static class McpSettingsCatalog
{
    public const string EnabledKey = "mcp-enabled";

    public static McpSettingsResponse ToResponse(HouseholdSetting? setting)
    {
        if (setting is null)
        {
            return new McpSettingsResponse(false);
        }

        return bool.TryParse(setting.Value, out var enabled)
            ? new McpSettingsResponse(enabled)
            : throw new InvalidOperationException($"Household setting '{EnabledKey}' does not contain a valid boolean.");
    }

    public static string Serialize(bool enabled) => enabled.ToString().ToLowerInvariant();
}
