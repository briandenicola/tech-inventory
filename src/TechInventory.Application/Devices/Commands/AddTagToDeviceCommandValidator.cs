using FluentValidation;

namespace TechInventory.Application.Devices.Commands;

public sealed class AddTagToDeviceCommandValidator : AbstractValidator<AddTagToDeviceCommand>
{
    public AddTagToDeviceCommandValidator()
    {
        RuleFor(command => command.DeviceId)
            .NotEmpty();

        RuleFor(command => command.TagId)
            .NotEmpty();
    }
}
