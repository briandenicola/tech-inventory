using FluentValidation;
using TechInventory.Application.Common.Validation;

namespace TechInventory.Application.Devices.Commands;

public sealed class CreateDeviceCommandValidator : AbstractValidator<CreateDeviceCommand>
{
    public CreateDeviceCommandValidator()
    {
        DeviceValidationRules.ApplySharedDeviceRules(
            this,
            command => command.Name,
            command => command.CurrencyCode,
            command => command.Model,
            command => command.SerialNumber,
            command => command.PurchasePrice,
            command => command.Notes,
            command => command.DisposalMethod,
            command => command.RetiredDate,
            command => command.Status);

        DeviceValidationRules.ApplyOptionalBrandRule(this, command => command.BrandId);

        DeviceValidationRules.ApplyRequiredReferenceRules(
            this,
            command => command.CategoryId,
            command => command.OwnerId,
            command => command.LocationId);

        DeviceValidationRules.ApplyOptionalNetworkRule(this, command => command.NetworkId);

        DeviceValidationRules.ApplyExtendedFieldRules(
            this,
            command => command.Purpose,
            command => command.OperatingSystem,
            command => command.IpAddress,
            command => command.MacAddress,
            command => command.ProductUrl,
            command => command.Version);
    }
}
