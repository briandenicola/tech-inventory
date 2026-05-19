using FluentValidation;

namespace TechInventory.Application.Devices.Commands;

public sealed class BulkDeleteDevicesCommandValidator : AbstractValidator<BulkDeleteDevicesCommand>
{
    public BulkDeleteDevicesCommandValidator()
    {
        RuleFor(command => command.DeviceIds)
            .NotNull()
            .NotEmpty()
            .WithMessage("At least one device id is required.")
            .Must(ids => ids.Count <= 500)
            .WithMessage("A bulk operation cannot affect more than 500 devices in a single request.");

        RuleForEach(command => command.DeviceIds)
            .NotEmpty()
            .WithMessage("DeviceIds cannot contain empty GUID values.");

        RuleFor(command => command.Reason)
            .NotEmpty()
            .WithMessage("Reason is required for bulk delete.")
            .MinimumLength(10)
            .WithMessage("Reason must be at least 10 characters.")
            .MaximumLength(1000);
    }
}
