using FluentValidation;
using TechInventory.Application.Common.Validation;

namespace TechInventory.Application.Devices.Commands;

public sealed class UpdateDeviceCommandValidator : AbstractValidator<UpdateDeviceCommand>
{
    public UpdateDeviceCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

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

        RuleFor(command => command.CurrencyCode)
            .Must(ValidationRules.BeValidCurrencyCode)
            .WithMessage("CurrencyCode must be a valid ISO 4217 code.");

        DeviceValidationRules.ApplyRequiredReferenceRules(
            this,
            command => command.BrandId,
            command => command.CategoryId,
            command => command.OwnerId,
            command => command.LocationId);

        DeviceValidationRules.ApplyOptionalNetworkRule(this, command => command.NetworkId);
    }
}
