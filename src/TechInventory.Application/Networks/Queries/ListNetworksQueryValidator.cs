using FluentValidation;

namespace TechInventory.Application.Networks.Queries;

public sealed class ListNetworksQueryValidator : AbstractValidator<ListNetworksQuery>
{
    public ListNetworksQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 200);
    }
}
