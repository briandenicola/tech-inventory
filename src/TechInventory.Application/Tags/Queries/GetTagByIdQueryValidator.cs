using FluentValidation;

namespace TechInventory.Application.Tags.Queries;

public sealed class GetTagByIdQueryValidator : AbstractValidator<GetTagByIdQuery>
{
    public GetTagByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
