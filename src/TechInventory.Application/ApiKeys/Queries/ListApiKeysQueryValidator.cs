using FluentValidation;

namespace TechInventory.Application.ApiKeys.Queries;

public sealed class ListApiKeysQueryValidator : AbstractValidator<ListApiKeysQuery>
{
    public ListApiKeysQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 200);
    }
}

public sealed class ListAllApiKeysQueryValidator : AbstractValidator<ListAllApiKeysQuery>
{
    public ListAllApiKeysQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 200);
    }
}
