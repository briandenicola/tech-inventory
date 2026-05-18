using FluentValidation;

namespace TechInventory.Application.Tags.Commands;

public sealed class DeleteTagCommandValidator : AbstractValidator<DeleteTagCommand>
{
    public DeleteTagCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();
    }
}
