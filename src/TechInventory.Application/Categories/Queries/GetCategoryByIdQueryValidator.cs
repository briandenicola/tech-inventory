using FluentValidation;

namespace TechInventory.Application.Categories.Queries;

public sealed class GetCategoryByIdQueryValidator : AbstractValidator<GetCategoryByIdQuery>
{
    public GetCategoryByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
