using TechInventory.Domain.Enums;
using TechInventory.Domain.Primitives;
using TechInventory.Domain.ValueObjects;

namespace TechInventory.Domain.Entities;

public sealed class Device(
    Guid id,
    string name,
    Guid brandId,
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
    string? disposalMethod = null) : AggregateRoot(id)
{
    public string Name { get; private set; } = Guard.AgainstNullOrWhiteSpace(name, nameof(name), 200);

    public string? Model { get; private set; } = Guard.AgainstMaxLength(model, nameof(model), 200);

    public string? SerialNumber { get; private set; } = Guard.AgainstMaxLength(serialNumber, nameof(serialNumber), 200);

    public Guid BrandId { get; private set; } = Guard.AgainstDefault(brandId, nameof(brandId));

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

    public static Device Create(
        Guid id,
        Household household,
        string name,
        Guid brandId,
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
        string? disposalMethod = null)
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
            disposalMethod);
    }

    public void UpdateDetails(
        string name,
        Guid brandId,
        Guid categoryId,
        Guid ownerId,
        Guid locationId,
        Currency currency,
        string? model = null,
        string? serialNumber = null,
        Guid? networkId = null,
        DateOnly? purchaseDate = null,
        decimal? purchasePrice = null,
        string? modifiedBy = null)
    {
        EnsureEditable();

        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name), 200);
        BrandId = Guard.AgainstDefault(brandId, nameof(brandId));
        CategoryId = Guard.AgainstDefault(categoryId, nameof(categoryId));
        OwnerId = Guard.AgainstDefault(ownerId, nameof(ownerId));
        LocationId = Guard.AgainstDefault(locationId, nameof(locationId));
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        Model = Guard.AgainstMaxLength(model, nameof(model), 200);
        SerialNumber = Guard.AgainstMaxLength(serialNumber, nameof(serialNumber), 200);
        NetworkId = Guard.AgainstOptionalDefault(networkId, nameof(networkId));
        PurchaseDate = purchaseDate;
        PurchasePrice = Guard.AgainstNegative(purchasePrice, nameof(purchasePrice));

        Touch(modifiedBy);
    }

    public void ChangeStatus(DeviceStatus status, DateOnly? retiredDate = null, string? disposalMethod = null, string? modifiedBy = null)
    {
        if (Status == DeviceStatus.Retired && status != DeviceStatus.Retired)
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
}
