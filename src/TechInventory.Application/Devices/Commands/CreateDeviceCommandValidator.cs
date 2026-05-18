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

        DeviceValidationRules.ApplyRequiredReferenceRules(
            this,
            command => command.BrandId,
            command => command.CategoryId,
            command => command.OwnerId,
            command => command.LocationId);

        DeviceValidationRules.ApplyOptionalNetworkRule(this, command => command.NetworkId);
    }
}
