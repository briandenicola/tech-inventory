using FluentValidation;

namespace TechInventory.Application.Owners.Queries;

public sealed class ListOwnersQueryValidator : AbstractValidator<ListOwnersQuery>
{
    public ListOwnersQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 200);
    }
}
