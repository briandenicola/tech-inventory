using FluentValidation;
using TechInventory.Application.Common.Validation;

namespace TechInventory.Application.Devices.Queries;

public sealed class ListDevicesQueryValidator : AbstractValidator<ListDevicesQuery>
{
    public ListDevicesQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 200);

        RuleFor(query => query.BrandId)
            .Must(ValidationRules.BeValidOptionalGuid)
            .WithMessage("BrandId must be a non-empty GUID when provided.");

        RuleFor(query => query.CategoryId)
            .Must(ValidationRules.BeValidOptionalGuid)
            .WithMessage("CategoryId must be a non-empty GUID when provided.");

        RuleFor(query => query.OwnerId)
            .Must(ValidationRules.BeValidOptionalGuid)
            .WithMessage("OwnerId must be a non-empty GUID when provided.");

        RuleFor(query => query.LocationId)
            .Must(ValidationRules.BeValidOptionalGuid)
            .WithMessage("LocationId must be a non-empty GUID when provided.");

        RuleFor(query => query.NetworkId)
            .Must(ValidationRules.BeValidOptionalGuid)
            .WithMessage("NetworkId must be a non-empty GUID when provided.");

        RuleFor(query => query.PurchaseYearFrom)
            .InclusiveBetween(1, 9999)
            .When(query => query.PurchaseYearFrom.HasValue);

        RuleFor(query => query.PurchaseYearTo)
            .InclusiveBetween(1, 9999)
            .When(query => query.PurchaseYearTo.HasValue);

        RuleFor(query => query)
            .Must(query => !query.PurchaseYearFrom.HasValue || !query.PurchaseYearTo.HasValue || query.PurchaseYearFrom <= query.PurchaseYearTo)
            .WithMessage("PurchaseYearFrom cannot be greater than PurchaseYearTo.");

        RuleFor(query => query.SortBy)
            .Must(sortBy => ValidationRules.BeValidSort(sortBy, "name", "purchaseDate", "createdAt"))
            .WithMessage("SortBy must be one of: name, purchaseDate, createdAt.");

        When(query => query.TagIds is not null, () =>
        {
            RuleForEach(query => query.TagIds!)
                .NotEmpty()
                .WithMessage("TagIds cannot contain empty GUID values.");
        });
    }
}
