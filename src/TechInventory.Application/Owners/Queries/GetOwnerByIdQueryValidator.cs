using FluentValidation;

namespace TechInventory.Application.Owners.Queries;

public sealed class GetOwnerByIdQueryValidator : AbstractValidator<GetOwnerByIdQuery>
{
    public GetOwnerByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
