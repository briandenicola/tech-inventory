using FluentValidation;

namespace TechInventory.Application.Owners.Commands;

public sealed class EnsureCurrentOwnerProvisionedCommandValidator : AbstractValidator<EnsureCurrentOwnerProvisionedCommand>
{
    public EnsureCurrentOwnerProvisionedCommandValidator()
    {
        RuleFor(command => command.EntraObjectId)
            .NotEmpty();

        RuleFor(command => command.DisplayName)
            .MaximumLength(200)
            .When(command => !string.IsNullOrWhiteSpace(command.DisplayName));
    }
}
