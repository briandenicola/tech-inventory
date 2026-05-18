using FluentValidation;

namespace TechInventory.Application.Locations.Queries;

public sealed class GetLocationByIdQueryValidator : AbstractValidator<GetLocationByIdQuery>
{
    public GetLocationByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
