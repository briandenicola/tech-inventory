using FluentValidation;

namespace TechInventory.Application.Locations.Commands;

public sealed class CreateLocationCommandValidator : AbstractValidator<CreateLocationCommand>
{
    public CreateLocationCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Type)
            .IsInEnum();
    }
}
