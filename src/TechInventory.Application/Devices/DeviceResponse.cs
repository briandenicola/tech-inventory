using TechInventory.Domain.Entities;

namespace TechInventory.Application.Devices;

public sealed record DeviceResponse(
    Guid Id,
    string Name,
    string? Model,
    string? SerialNumber,
    Guid? BrandId,
    Guid CategoryId,
    Guid OwnerId,
    Guid LocationId,
    Guid? NetworkId,
    DateOnly? PurchaseDate,
    decimal? PurchasePrice,
    string CurrencyCode,
    string Status,
    string? Notes,
    DateOnly? RetiredDate,
    string? DisposalMethod,
    string? Purpose,
    string? OperatingSystem,
    string? IpAddress,
    string? MacAddress,
    string? ProductUrl,
    string? Version,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset ModifiedAt,
    string? ModifiedBy)
{
    public static DeviceResponse FromEntity(Device device)
    {
        ArgumentNullException.ThrowIfNull(device);

        return new DeviceResponse(
            device.Id,
            device.Name,
            device.Model,
            device.SerialNumber,
            device.BrandId,
            device.CategoryId,
            device.OwnerId,
            device.LocationId,
            device.NetworkId,
            device.PurchaseDate,
            device.PurchasePrice,
            device.Currency.Code,
            device.Status.ToString(),
            device.Notes,
            device.RetiredDate,
            device.DisposalMethod,
            device.Purpose,
            device.OperatingSystem,
            device.IpAddress,
            device.MacAddress,
            device.ProductUrl,
            device.Version,
            device.CreatedAt,
            device.CreatedBy,
            device.ModifiedAt,
            device.ModifiedBy);
    }
}
