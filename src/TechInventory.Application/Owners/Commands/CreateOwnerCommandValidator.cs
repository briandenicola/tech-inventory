using FluentValidation;
using TechInventory.Application.Common.Validation;

namespace TechInventory.Application.Owners.Commands;

public sealed class CreateOwnerCommandValidator : AbstractValidator<CreateOwnerCommand>
{
    public CreateOwnerCommandValidator()
    {
        RuleFor(command => command.DisplayName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Role)
            .IsInEnum();

        RuleFor(command => command.EntraObjectId)
            .Must(ValidationRules.BeValidOptionalGuid)
            .WithMessage("EntraObjectId must be a non-empty GUID when provided.");
    }
}
