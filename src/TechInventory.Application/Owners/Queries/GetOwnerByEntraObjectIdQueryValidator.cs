using FluentValidation;

namespace TechInventory.Application.Owners.Queries;

public sealed class GetOwnerByEntraObjectIdQueryValidator : AbstractValidator<GetOwnerByEntraObjectIdQuery>
{
    public GetOwnerByEntraObjectIdQueryValidator()
    {
        RuleFor(x => x.EntraObjectId)
            .NotEmpty()
            .WithMessage("EntraObjectId is required.");
    }
}
