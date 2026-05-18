using FluentValidation;
using TechInventory.Application.Common.Validation;

namespace TechInventory.Application.Categories.Commands;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.ParentId)
            .Must(ValidationRules.BeValidOptionalGuid)
            .WithMessage("ParentId must be a non-empty GUID when provided.");

        RuleFor(command => command.Icon)
            .MaximumLength(100);
    }
}
