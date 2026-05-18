namespace TechInventory.Application.Imports;

internal static class ImportFieldNames
{
    public const string Name = "Name";
    public const string Brand = "Brand";
    public const string Category = "Category";
    public const string Owner = "Owner";
    public const string Location = "Location";
    public const string Network = "Network";
    public const string Model = "Model";
    public const string SerialNumber = "SerialNumber";
    public const string PurchaseDate = "PurchaseDate";
    public const string PurchasePrice = "PurchasePrice";
    public const string Status = "Status";
    public const string Notes = "Notes";
    public const string CurrencyCode = "CurrencyCode";
    public const string RetiredDate = "RetiredDate";
    public const string DisposalMethod = "DisposalMethod";
    public const string Purpose = "Purpose";
    public const string OperatingSystem = "OperatingSystem";
    public const string IpAddress = "IpAddress";
    public const string MacAddress = "MacAddress";
    public const string ProductUrl = "ProductUrl";
    public const string Version = "Version";
    public const string DeviceName = "DeviceName";
    public const string DeviceType = "DeviceType";
    public const string Vendor = "Vendor";
    public const string Url = "Url";
    public const string PurchasedDate = "PurchasedDate";
    public const string Retired = "Retired";
    public const string Networking = "Networking";

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        [Name] = Name,
        ["Title"] = Name,
        ["Device Name"] = Name,
        ["DeviceName"] = Name,
        [Brand] = Brand,
        ["Vendor"] = Brand,
        [Category] = Category,
        ["Device Type"] = Category,
        ["DeviceType"] = Category,
        [Owner] = Owner,
        [Location] = Location,
        [Network] = Network,
        ["Networking"] = Network,
        [Model] = Model,
        [SerialNumber] = SerialNumber,
        ["Serial Number"] = SerialNumber,
        [PurchaseDate] = PurchaseDate,
        ["Purchase Date"] = PurchaseDate,
        ["PurchasedDate"] = PurchaseDate,
        ["Purchased Date"] = PurchaseDate,
        [PurchasePrice] = PurchasePrice,
        ["Purchase Price"] = PurchasePrice,
        [Status] = Status,
        ["Retired"] = Status,
        [Notes] = Notes,
        [CurrencyCode] = CurrencyCode,
        ["Currency"] = CurrencyCode,
        ["Currency Code"] = CurrencyCode,
        [RetiredDate] = RetiredDate,
        ["Retired Date"] = RetiredDate,
        [DisposalMethod] = DisposalMethod,
        ["Disposal Method"] = DisposalMethod,
        [Purpose] = Purpose,
        [OperatingSystem] = OperatingSystem,
        ["Operating System"] = OperatingSystem,
        [IpAddress] = IpAddress,
        ["IP Address"] = IpAddress,
        [MacAddress] = MacAddress,
        ["MAC Address"] = MacAddress,
        [ProductUrl] = ProductUrl,
        ["Product Url"] = ProductUrl,
        ["Url"] = ProductUrl,
        [Version] = Version,
    };

    public static IReadOnlyCollection<string> SupportedFields { get; } =
    [
        Name,
        Brand,
        Category,
        Owner,
        Location,
        Network,
        Model,
        SerialNumber,
        PurchaseDate,
        PurchasePrice,
        Status,
        Notes,
        CurrencyCode,
        RetiredDate,
        DisposalMethod,
        Purpose,
        OperatingSystem,
        IpAddress,
        MacAddress,
        ProductUrl,
        Version,
    ];

    public static bool TryNormalize(string value, out string normalizedField)
    {
        if (Aliases.TryGetValue(value.Trim(), out var match))
        {
            normalizedField = match;
            return true;
        }

        normalizedField = null!;
        return false;
    }

    public static string NormalizeRequired(string value)
    {
        if (!TryNormalize(value, out var normalizedField))
        {
            throw new ArgumentException($"Unsupported import field '{value}'.", nameof(value));
        }

        return normalizedField;
    }
}
