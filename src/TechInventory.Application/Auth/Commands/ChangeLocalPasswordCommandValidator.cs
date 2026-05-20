using FluentValidation;

namespace TechInventory.Application.Auth.Commands;

public sealed class ChangeLocalPasswordCommandValidator : AbstractValidator<ChangeLocalPasswordCommand>
{
    public const int MinPasswordLength = 12;
    public const int MaxPasswordLength = 256;

    public ChangeLocalPasswordCommandValidator()
    {
        RuleFor(command => command.LocalUserId)
            .NotEmpty();

        RuleFor(command => command.CurrentPassword)
            .NotEmpty()
            .MaximumLength(1024);

        RuleFor(command => command.NewPassword)
            .NotEmpty()
            .MinimumLength(MinPasswordLength)
                .WithMessage($"New password must be at least {MinPasswordLength} characters.")
            .MaximumLength(MaxPasswordLength);
    }
}
