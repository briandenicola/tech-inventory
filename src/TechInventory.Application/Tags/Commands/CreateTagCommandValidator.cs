using FluentValidation;

namespace TechInventory.Application.Tags.Commands;

public sealed class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Color)
            .MaximumLength(32);
    }
}
