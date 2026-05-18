using FluentValidation;

namespace TechInventory.Application.Brands.Queries;

public sealed class ListBrandsQueryValidator : AbstractValidator<ListBrandsQuery>
{
    public ListBrandsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 200);
    }
}
