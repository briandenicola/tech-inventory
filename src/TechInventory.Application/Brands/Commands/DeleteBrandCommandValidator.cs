using FluentValidation;

namespace TechInventory.Application.Brands.Commands;

public sealed class DeleteBrandCommandValidator : AbstractValidator<DeleteBrandCommand>
{
    public DeleteBrandCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();
    }
}
