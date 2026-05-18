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

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        [Name] = Name,
        ["Title"] = Name,
        ["Device Name"] = Name,
        [Brand] = Brand,
        [Category] = Category,
        [Owner] = Owner,
        [Location] = Location,
        [Network] = Network,
        [Model] = Model,
        [SerialNumber] = SerialNumber,
        ["Serial Number"] = SerialNumber,
        [PurchaseDate] = PurchaseDate,
        ["Purchase Date"] = PurchaseDate,
        [PurchasePrice] = PurchasePrice,
        ["Purchase Price"] = PurchasePrice,
        [Status] = Status,
        [Notes] = Notes,
        [CurrencyCode] = CurrencyCode,
        ["Currency"] = CurrencyCode,
        ["Currency Code"] = CurrencyCode,
        [RetiredDate] = RetiredDate,
        ["Retired Date"] = RetiredDate,
        [DisposalMethod] = DisposalMethod,
        ["Disposal Method"] = DisposalMethod,
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
