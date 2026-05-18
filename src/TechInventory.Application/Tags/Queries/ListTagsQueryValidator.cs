using FluentValidation;

namespace TechInventory.Application.Tags.Queries;

public sealed class ListTagsQueryValidator : AbstractValidator<ListTagsQuery>
{
    public ListTagsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 200);
    }
}
