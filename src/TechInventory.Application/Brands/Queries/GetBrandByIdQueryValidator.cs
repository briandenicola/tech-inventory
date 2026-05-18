using FluentValidation;

namespace TechInventory.Application.Brands.Queries;

public sealed class GetBrandByIdQueryValidator : AbstractValidator<GetBrandByIdQuery>
{
    public GetBrandByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
