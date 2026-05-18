using FluentValidation;

namespace TechInventory.Application.Brands.Commands;

public sealed class UpdateBrandCommandValidator : AbstractValidator<UpdateBrandCommand>
{
    public UpdateBrandCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Website)
            .Must(website => string.IsNullOrWhiteSpace(website) || Uri.TryCreate(website, UriKind.Absolute, out _))
            .WithMessage("Website must be an absolute URI when provided.")
            .MaximumLength(2048);

        RuleFor(command => command.Notes)
            .MaximumLength(4000);
    }
}
