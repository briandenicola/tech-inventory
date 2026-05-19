using FluentValidation;

namespace TechInventory.Application.Devices.Commands;

public sealed class BulkUpdateDevicesCommandValidator : AbstractValidator<BulkUpdateDevicesCommand>
{
    public BulkUpdateDevicesCommandValidator()
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

        RuleFor(command => command.Changes)
            .NotNull()
            .Must(HasAtLeastOneChange)
            .WithMessage("At least one field must be set on Changes.");
    }

    private static bool HasAtLeastOneChange(BulkUpdateDeviceChanges? changes)
        => changes is not null
            && (changes.CategoryId.HasValue
                || changes.OwnerId.HasValue
                || changes.BrandId.HasValue
                || changes.LocationId.HasValue
                || changes.Status.HasValue);
}
