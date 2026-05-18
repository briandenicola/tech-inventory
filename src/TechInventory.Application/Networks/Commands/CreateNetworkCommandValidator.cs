using FluentValidation;

namespace TechInventory.Application.Networks.Commands;

public sealed class CreateNetworkCommandValidator : AbstractValidator<CreateNetworkCommand>
{
    public CreateNetworkCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Description)
            .MaximumLength(1000);
    }
}
