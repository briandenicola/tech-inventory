using FluentValidation;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Auth.Commands;

public sealed class LocalLoginCommandValidator : AbstractValidator<LocalLoginCommand>
{
    public LocalLoginCommandValidator()
    {
        RuleFor(command => command.Username)
            .NotEmpty()
            .MaximumLength(LocalUser.MaxUsernameLength);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MaximumLength(1024);
    }
}
