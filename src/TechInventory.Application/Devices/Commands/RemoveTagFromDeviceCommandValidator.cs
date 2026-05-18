using FluentValidation;

namespace TechInventory.Application.Devices.Commands;

public sealed class RemoveTagFromDeviceCommandValidator : AbstractValidator<RemoveTagFromDeviceCommand>
{
    public RemoveTagFromDeviceCommandValidator()
    {
        RuleFor(command => command.DeviceId)
            .NotEmpty();

        RuleFor(command => command.TagId)
            .NotEmpty();
    }
}
