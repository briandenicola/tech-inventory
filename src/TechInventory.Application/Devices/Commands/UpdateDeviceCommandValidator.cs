using FluentValidation;
using TechInventory.Application.Common.Validation;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Devices.Commands;

public sealed class UpdateDeviceCommandValidator : AbstractValidator<UpdateDeviceCommand>
{
    public UpdateDeviceCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.BrandId)
            .NotEmpty()
            .WithMessage("BrandId is required.");

        RuleFor(command => command.CategoryId)
            .NotEmpty()
            .WithMessage("CategoryId is required.");

        RuleFor(command => command.OwnerId)
            .NotEmpty()
            .WithMessage("OwnerId is required.");

        RuleFor(command => command.LocationId)
            .NotEmpty()
            .WithMessage("LocationId is required.");

        RuleFor(command => command.CurrencyCode)
            .Must(ValidationRules.BeValidCurrencyCode)
            .WithMessage("CurrencyCode must be a valid ISO 4217 code.");

        RuleFor(command => command.NetworkId)
            .Must(ValidationRules.BeValidOptionalGuid)
            .WithMessage("NetworkId must be a non-empty GUID when provided.");

        RuleFor(command => command.Model)
            .MaximumLength(200);

        RuleFor(command => command.SerialNumber)
            .MaximumLength(200);

        RuleFor(command => command.PurchasePrice)
            .GreaterThanOrEqualTo(0)
            .When(command => command.PurchasePrice.HasValue);

        RuleFor(command => command.Notes)
            .MaximumLength(4000);

        RuleFor(command => command.DisposalMethod)
            .MaximumLength(500);

        RuleFor(command => command)
            .Must(command => !command.RetiredDate.HasValue || command.Status is DeviceStatus.Retired or DeviceStatus.Disposed)
            .WithMessage("RetiredDate can only be set when Status is Retired or Disposed.");

        RuleFor(command => command)
            .Must(command => string.IsNullOrWhiteSpace(command.DisposalMethod) || command.Status is DeviceStatus.Retired or DeviceStatus.Disposed)
            .WithMessage("DisposalMethod can only be set when Status is Retired or Disposed.");
    }
}
