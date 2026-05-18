using FluentValidation;

namespace TechInventory.Application.Devices.Commands;

public sealed class DeleteDeviceCommandValidator : AbstractValidator<DeleteDeviceCommand>
{
    public DeleteDeviceCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.DisposalMethod)
            .MaximumLength(500);
    }
}
