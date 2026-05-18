using FluentValidation;

namespace TechInventory.Application.Owners.Commands;

public sealed class DeleteOwnerCommandValidator : AbstractValidator<DeleteOwnerCommand>
{
    public DeleteOwnerCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();
    }
}
