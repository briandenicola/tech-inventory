using FluentValidation;
using TechInventory.Application.Common.Validation;

namespace TechInventory.Application.Owners.Commands;

public sealed class UpdateOwnerCommandValidator : AbstractValidator<UpdateOwnerCommand>
{
    public UpdateOwnerCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

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
