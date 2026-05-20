using System.Text.Json;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Settings;

public static class DisplaySettingsCatalog
{
    public const string DeviceListColumnsKey = "device-list-columns";
    public const string DeviceDetailFieldsKey = "device-detail-fields";

    public static readonly string[] DefaultDeviceListColumns =
    [
        "name",
        "brand",
        "category",
        "purchaseDate",
        "location",
        "status"
    ];

    public static readonly string[] DefaultDeviceDetailFields =
    [
        "brand",
        "model",
        "category",
        "purchaseDate",
        "location",
        "owner",
        "network",
        "notes",
        "status"
    ];

    private static readonly HashSet<string> AllowedDeviceListColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "name",
        "brand",
        "model",
        "category",
        "owner",
        "location",
        "network",
        "status",
        "purchaseDate",
        "purchasePrice",
        "warrantyExpiry",
        "serialNumber"
    };

    private static readonly HashSet<string> AllowedDeviceDetailFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "brand",
        "model",
        "category",
        "owner",
        "location",
        "network",
        "status",
        "purchaseDate",
        "purchasePrice",
        "warrantyExpiry",
        "serialNumber",
        "notes",
        "operatingSystem",
        "version",
        "ipAddress",
        "macAddress",
        "productUrl",
        "retiredDate",
        "disposalMethod",
        "purpose"
    };

    public static DisplaySettingsResponse GetDefaultResponse()
        => new([.. DefaultDeviceListColumns], [.. DefaultDeviceDetailFields]);

    public static bool ContainsRequiredListColumn(IEnumerable<string> columns)
        => NormalizeColumns(columns).Any(column => string.Equals(column, "name", StringComparison.OrdinalIgnoreCase));

    public static bool HasDuplicates(IEnumerable<string> columns)
    {
        var normalized = NormalizeColumns(columns);
        return normalized.Length != normalized.Distinct(StringComparer.OrdinalIgnoreCase).Count();
    }

    public static bool IsAllowedDeviceListColumn(string column)
        => !string.IsNullOrWhiteSpace(column) && AllowedDeviceListColumns.Contains(column.Trim());

    public static bool IsAllowedDeviceDetailField(string column)
        => !string.IsNullOrWhiteSpace(column) && AllowedDeviceDetailFields.Contains(column.Trim());

    public static string[] NormalizeColumns(IEnumerable<string> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);

        return columns
            .Select(column => column?.Trim())
            .Where(column => !string.IsNullOrWhiteSpace(column))
            .Cast<string>()
            .ToArray();
    }

    public static string SerializeColumns(IEnumerable<string> columns)
        => JsonSerializer.Serialize(NormalizeColumns(columns));

    public static HouseholdSetting CreateDefaultSetting(Guid householdId, string settingKey)
        => new(Guid.NewGuid(), householdId, settingKey, SerializeColumns(GetDefaultColumns(settingKey)));

    public static DisplaySettingsResponse ToResponse(IReadOnlyDictionary<string, HouseholdSetting> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var listColumns = settings.TryGetValue(DeviceListColumnsKey, out var listSetting)
            ? DeserializeColumns(listSetting.Value, DeviceListColumnsKey)
            : [.. DefaultDeviceListColumns];
        var detailFields = settings.TryGetValue(DeviceDetailFieldsKey, out var detailSetting)
            ? DeserializeColumns(detailSetting.Value, DeviceDetailFieldsKey)
            : [.. DefaultDeviceDetailFields];

        return new DisplaySettingsResponse(listColumns, detailFields);
    }

    private static string[] DeserializeColumns(string value, string settingKey)
    {
        try
        {
            var columns = JsonSerializer.Deserialize<string[]>(value)
                ?? throw new InvalidOperationException($"Household setting '{settingKey}' does not contain a valid JSON array.");

            var normalized = NormalizeColumns(columns);
            EnsureValidColumns(normalized, settingKey);
            return normalized;
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Household setting '{settingKey}' does not contain valid JSON.", exception);
        }
    }

    private static void EnsureValidColumns(IReadOnlyList<string> columns, string settingKey)
    {
        if (columns.Count == 0)
        {
            throw new InvalidOperationException($"Household setting '{settingKey}' must contain at least one column identifier.");
        }

        if (columns.Count != columns.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            throw new InvalidOperationException($"Household setting '{settingKey}' contains duplicate column identifiers.");
        }

        switch (settingKey)
        {
            case DeviceListColumnsKey:
                if (!ContainsRequiredListColumn(columns))
                {
                    throw new InvalidOperationException("Household setting 'device-list-columns' must include 'name'.");
                }

                if (columns.Any(column => !IsAllowedDeviceListColumn(column)))
                {
                    throw new InvalidOperationException("Household setting 'device-list-columns' contains an unknown column identifier.");
                }

                break;
            case DeviceDetailFieldsKey:
                if (columns.Any(column => !IsAllowedDeviceDetailField(column)))
                {
                    throw new InvalidOperationException("Household setting 'device-detail-fields' contains an unknown column identifier.");
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(settingKey), settingKey, "Unknown display settings key.");
        }
    }

    private static string[] GetDefaultColumns(string settingKey)
    {
        return settingKey switch
        {
            DeviceListColumnsKey => [.. DefaultDeviceListColumns],
            DeviceDetailFieldsKey => [.. DefaultDeviceDetailFields],
            _ => throw new ArgumentOutOfRangeException(nameof(settingKey), settingKey, "Unknown display settings key.")
        };
    }
}
