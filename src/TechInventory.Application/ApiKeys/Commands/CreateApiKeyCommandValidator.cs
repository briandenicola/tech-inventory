using FluentValidation;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.ApiKeys.Commands;

public sealed class CreateApiKeyCommandValidator : AbstractValidator<CreateApiKeyCommand>
{
    public CreateApiKeyCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(ApiKey.MaxNameLength);

        RuleFor(command => command.Scope)
            .IsInEnum();

        // Non-expiring keys are not permitted (ADR 0003), so null means "use the
        // default", never "never expires".
        RuleFor(command => command.ExpiresInDays!.Value)
            .InclusiveBetween(1, ApiKey.MaxExpiryDays)
            .When(command => command.ExpiresInDays.HasValue);
    }
}
