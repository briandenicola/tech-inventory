using FluentValidation;

namespace TechInventory.Application.Devices.Commands;

public sealed class ClaimDeviceOwnershipCommandValidator : AbstractValidator<ClaimDeviceOwnershipCommand>
{
    public ClaimDeviceOwnershipCommandValidator()
    {
        RuleFor(command => command.DeviceId)
            .NotEmpty();

        RuleFor(command => command.OwnerId)
            .NotEmpty();
    }
}
