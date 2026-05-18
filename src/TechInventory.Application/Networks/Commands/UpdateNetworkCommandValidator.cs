using FluentValidation;

namespace TechInventory.Application.Networks.Commands;

public sealed class UpdateNetworkCommandValidator : AbstractValidator<UpdateNetworkCommand>
{
    public UpdateNetworkCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Description)
            .MaximumLength(1000);
    }
}
