using FluentValidation;

namespace TechInventory.Application.Locations.Commands;

public sealed class DeleteLocationCommandValidator : AbstractValidator<DeleteLocationCommand>
{
    public DeleteLocationCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();
    }
}
