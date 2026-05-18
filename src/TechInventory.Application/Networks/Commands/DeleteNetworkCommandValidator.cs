using FluentValidation;

namespace TechInventory.Application.Networks.Commands;

public sealed class DeleteNetworkCommandValidator : AbstractValidator<DeleteNetworkCommand>
{
    public DeleteNetworkCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();
    }
}
