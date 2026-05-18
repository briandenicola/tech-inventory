using FluentValidation;

namespace TechInventory.Application.Devices.Queries;

public sealed class GetDeviceByIdQueryValidator : AbstractValidator<GetDeviceByIdQuery>
{
    public GetDeviceByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
