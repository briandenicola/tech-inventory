using FluentValidation;

namespace TechInventory.Application.ApiKeys.Commands;

public sealed class RevokeApiKeyCommandValidator : AbstractValidator<RevokeApiKeyCommand>
{
    public RevokeApiKeyCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
