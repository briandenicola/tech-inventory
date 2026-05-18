using FluentValidation;

namespace TechInventory.Application.Networks.Queries;

public sealed class GetNetworkByIdQueryValidator : AbstractValidator<GetNetworkByIdQuery>
{
    public GetNetworkByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
