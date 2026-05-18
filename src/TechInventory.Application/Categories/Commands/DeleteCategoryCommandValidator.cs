using FluentValidation;

namespace TechInventory.Application.Categories.Commands;

public sealed class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();
    }
}
