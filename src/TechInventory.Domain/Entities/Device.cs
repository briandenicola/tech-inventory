using TechInventory.Domain.Enums;
using TechInventory.Domain.Primitives;
using TechInventory.Domain.ValueObjects;

namespace TechInventory.Domain.Entities;

public sealed class Device(
    Guid id,
    string name,
    Guid? brandId,
    Guid categoryId,
    Guid ownerId,
    Guid locationId,
    Currency currency,
    string? model = null,
    string? serialNumber = null,
    Guid? networkId = null,
    DateOnly? purchaseDate = null,
    decimal? purchasePrice = null,
    DeviceStatus status = DeviceStatus.Active,
    string? notes = null,
    DateOnly? retiredDate = null,
    string? disposalMethod = null,
    string? purpose = null,
    string? operatingSystem = null,
    string? ipAddress = null,
    string? macAddress = null,
    string? productUrl = null,
    string? version = null,
    DateOnly? warrantyExpiry = null) : AggregateRoot(id)
{
    public string Name { get; private set; } = Guard.AgainstNullOrWhiteSpace(name, nameof(name), 200);

    public string? Model { get; private set; } = Guard.AgainstMaxLength(model, nameof(model), 200);

    public string? SerialNumber { get; private set; } = Guard.AgainstMaxLength(serialNumber, nameof(serialNumber), 200);

    public Guid? BrandId { get; private set; } = Guard.AgainstOptionalDefault(brandId, nameof(brandId));

    public Guid CategoryId { get; private set; } = Guard.AgainstDefault(categoryId, nameof(categoryId));

    public Guid OwnerId { get; private set; } = Guard.AgainstDefault(ownerId, nameof(ownerId));

    public Guid LocationId { get; private set; } = Guard.AgainstDefault(locationId, nameof(locationId));

    public Guid? NetworkId { get; private set; } = Guard.AgainstOptionalDefault(networkId, nameof(networkId));

    public DateOnly? PurchaseDate { get; private set; } = purchaseDate;

    public decimal? PurchasePrice { get; private set; } = Guard.AgainstNegative(purchasePrice, nameof(purchasePrice));

    public Currency Currency { get; private set; } = currency ?? throw new ArgumentNullException(nameof(currency));

    public DeviceStatus Status { get; private set; } = status;

    public string? Notes { get; private set; } = Guard.AgainstMaxLength(notes, nameof(notes), 4000);

    public DateOnly? RetiredDate { get; private set; } = ValidateRetiredDate(status, retiredDate);

    public string? DisposalMethod { get; private set; } = ValidateDisposalMethod(status, disposalMethod);

    public string? Purpose { get; private set; } = Guard.AgainstMaxLength(purpose, nameof(purpose), 500);

    public string? OperatingSystem { get; private set; } = Guard.AgainstMaxLength(operatingSystem, nameof(operatingSystem), 100);

    public string? IpAddress { get; private set; } = Guard.AgainstMaxLength(ipAddress, nameof(ipAddress), 45);

    public string? MacAddress { get; private set; } = ValidateMacAddress(macAddress);

    public string? ProductUrl { get; private set; } = ValidateProductUrl(productUrl);

    public string? Version { get; private set; } = Guard.AgainstMaxLength(version, nameof(version), 50);

    public DateOnly? WarrantyExpiry { get; private set; } = ValidateWarrantyExpiry(purchaseDate, warrantyExpiry);

    public static Device Create(
        Guid id,
        Household household,
        string name,
        Guid? brandId,
        Guid categoryId,
        Guid ownerId,
        Guid locationId,
        string? model = null,
        string? serialNumber = null,
        Guid? networkId = null,
        DateOnly? purchaseDate = null,
        decimal? purchasePrice = null,
        Currency? currency = null,
        DeviceStatus status = DeviceStatus.Active,
        string? notes = null,
        DateOnly? retiredDate = null,
        string? disposalMethod = null,
        string? purpose = null,
        string? operatingSystem = null,
        string? ipAddress = null,
        string? macAddress = null,
        string? productUrl = null,
        string? version = null,
        DateOnly? warrantyExpiry = null)
    {
        ArgumentNullException.ThrowIfNull(household);

        return new Device(
            id,
            name,
            brandId,
            categoryId,
            ownerId,
            locationId,
            currency ?? household.DefaultCurrency,
            model,
            serialNumber,
            networkId,
            purchaseDate,
            purchasePrice,
            status,
            notes,
            retiredDate,
            disposalMethod,
            purpose,
            operatingSystem,
            ipAddress,
            macAddress,
            productUrl,
            version,
            warrantyExpiry);
    }

    public void UpdateDetails(
        string name,
        Guid? brandId,
        Guid categoryId,
        Guid ownerId,
        Guid locationId,
        Currency currency,
        string? model = null,
        string? serialNumber = null,
        Guid? networkId = null,
        DateOnly? purchaseDate = null,
        decimal? purchasePrice = null,
        string? modifiedBy = null,
        string? purpose = null,
        string? operatingSystem = null,
        string? ipAddress = null,
        string? macAddress = null,
        string? productUrl = null,
        string? version = null,
        DateOnly? warrantyExpiry = null)
    {
        EnsureEditable();

        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name), 200);
        BrandId = Guard.AgainstOptionalDefault(brandId, nameof(brandId));
        CategoryId = Guard.AgainstDefault(categoryId, nameof(categoryId));
        OwnerId = Guard.AgainstDefault(ownerId, nameof(ownerId));
        LocationId = Guard.AgainstDefault(locationId, nameof(locationId));
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        Model = Guard.AgainstMaxLength(model, nameof(model), 200);
        SerialNumber = Guard.AgainstMaxLength(serialNumber, nameof(serialNumber), 200);
        NetworkId = Guard.AgainstOptionalDefault(networkId, nameof(networkId));
        PurchaseDate = purchaseDate;
        PurchasePrice = Guard.AgainstNegative(purchasePrice, nameof(purchasePrice));
        Purpose = Guard.AgainstMaxLength(purpose, nameof(purpose), 500);
        OperatingSystem = Guard.AgainstMaxLength(operatingSystem, nameof(operatingSystem), 100);
        IpAddress = Guard.AgainstMaxLength(ipAddress, nameof(ipAddress), 45);
        MacAddress = ValidateMacAddress(macAddress);
        ProductUrl = ValidateProductUrl(productUrl);
        Version = Guard.AgainstMaxLength(version, nameof(version), 50);
        WarrantyExpiry = ValidateWarrantyExpiry(PurchaseDate, warrantyExpiry);

        Touch(modifiedBy);
    }

    public void ChangeStatus(DeviceStatus status, DateOnly? retiredDate = null, string? disposalMethod = null, string? modifiedBy = null)
    {
        if (Status == DeviceStatus.Retired && status is not DeviceStatus.Retired and not DeviceStatus.Disposed)
        {
            throw new InvalidOperationException("Retired devices are read-only except for notes and disposal method.");
        }

        Status = status;
        RetiredDate = ValidateRetiredDate(status, retiredDate ?? RetiredDate);
        DisposalMethod = ValidateDisposalMethod(status, disposalMethod ?? DisposalMethod);
        Touch(modifiedBy);
    }

    public void UpdateNotes(string? notes, string? modifiedBy = null)
    {
        Notes = Guard.AgainstMaxLength(notes, nameof(notes), 4000);
        Touch(modifiedBy);
    }

    public void ReassignBrand(Guid? brandId, string? modifiedBy = null)
    {
        BrandId = Guard.AgainstOptionalDefault(brandId, nameof(brandId));
        Touch(modifiedBy);
    }

    public void ReassignCategory(Guid categoryId, string? modifiedBy = null)
    {
        CategoryId = Guard.AgainstDefault(categoryId, nameof(categoryId));
        Touch(modifiedBy);
    }

    public void ReassignLocation(Guid locationId, string? modifiedBy = null)
    {
        LocationId = Guard.AgainstDefault(locationId, nameof(locationId));
        Touch(modifiedBy);
    }

    public void ReassignNetwork(Guid? networkId, string? modifiedBy = null)
    {
        NetworkId = Guard.AgainstOptionalDefault(networkId, nameof(networkId));
        Touch(modifiedBy);
    }

    public void UpdateDisposalMethod(string? disposalMethod, string? modifiedBy = null)
    {
        if (Status is not DeviceStatus.Retired and not DeviceStatus.Disposed)
        {
            throw new InvalidOperationException("Disposal method can only be set for retired or disposed devices.");
        }

        DisposalMethod = ValidateDisposalMethod(Status, disposalMethod);
        Touch(modifiedBy);
    }

    private void EnsureEditable()
    {
        if (Status == DeviceStatus.Retired)
        {
            throw new InvalidOperationException("Retired devices are read-only except for notes and disposal method.");
        }
    }

    private static DateOnly? ValidateRetiredDate(DeviceStatus status, DateOnly? retiredDate)
    {
        if (retiredDate.HasValue && status is not DeviceStatus.Retired and not DeviceStatus.Disposed)
        {
            throw new ArgumentException("RetiredDate can only be set when the device is retired or disposed.", nameof(retiredDate));
        }

        if (!retiredDate.HasValue && status is DeviceStatus.Retired or DeviceStatus.Disposed)
        {
            return DateOnly.FromDateTime(DateTime.UtcNow);
        }

        return retiredDate;
    }

    private static string? ValidateDisposalMethod(DeviceStatus status, string? disposalMethod)
    {
        var normalized = Guard.AgainstMaxLength(disposalMethod, nameof(disposalMethod), 500);
        if (normalized is not null && status is not DeviceStatus.Retired and not DeviceStatus.Disposed)
        {
            throw new ArgumentException("DisposalMethod can only be set when the device is retired or disposed.", nameof(disposalMethod));
        }

        return normalized;
    }

    private static DateOnly? ValidateWarrantyExpiry(DateOnly? purchaseDate, DateOnly? warrantyExpiry)
    {
        if (warrantyExpiry.HasValue && purchaseDate.HasValue && warrantyExpiry.Value < purchaseDate.Value)
        {
            throw new ArgumentException("WarrantyExpiry cannot be earlier than PurchaseDate.", nameof(warrantyExpiry));
        }

        return warrantyExpiry;
    }

    private static string? ValidateMacAddress(string? macAddress)
    {
        var normalized = Guard.AgainstMaxLength(macAddress, nameof(macAddress), 17);
        if (normalized is null)
        {
            return null;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[0-9A-Fa-f]{2}(:[0-9A-Fa-f]{2}){5}$"))
        {
            throw new ArgumentException("MacAddress must be in format XX:XX:XX:XX:XX:XX.", nameof(macAddress));
        }

        return normalized.ToUpperInvariant();
    }

    private static string? ValidateProductUrl(string? productUrl)
    {
        var normalized = Guard.AgainstMaxLength(productUrl, nameof(productUrl), 500);
        if (normalized is null)
        {
            return null;
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out _))
        {
            throw new ArgumentException("ProductUrl must be a valid absolute URI.", nameof(productUrl));
        }

        return normalized;
    }
}
