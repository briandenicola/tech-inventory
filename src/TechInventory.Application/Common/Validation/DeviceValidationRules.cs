using System.Linq.Expressions;
using FluentValidation;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Common.Validation;

internal static class DeviceValidationRules
{
    public static void ApplySharedDeviceRules<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, string>> nameExpression,
        Expression<Func<T, string?>> currencyCodeExpression,
        Expression<Func<T, string?>> modelExpression,
        Expression<Func<T, string?>> serialNumberExpression,
        Expression<Func<T, decimal?>> purchasePriceExpression,
        Expression<Func<T, string?>> notesExpression,
        Expression<Func<T, string?>> disposalMethodExpression,
        Expression<Func<T, DateOnly?>> retiredDateExpression,
        Expression<Func<T, DeviceStatus>> statusExpression)
    {
        ArgumentNullException.ThrowIfNull(validator);

        var statusAccessor = statusExpression.Compile();

        validator.RuleFor(nameExpression)
            .NotEmpty()
            .MaximumLength(200);

        validator.RuleFor(currencyCodeExpression)
            .Must(ValidationRules.BeValidOptionalCurrencyCode)
            .WithMessage("CurrencyCode must be a valid ISO 4217 code when provided.");

        validator.RuleFor(modelExpression)
            .MaximumLength(200);

        validator.RuleFor(serialNumberExpression)
            .MaximumLength(200);

        validator.RuleFor(purchasePriceExpression)
            .GreaterThanOrEqualTo(0)
            .When(model => purchasePriceExpression.Compile()(model).HasValue);

        validator.RuleFor(notesExpression)
            .MaximumLength(4000);

        validator.RuleFor(disposalMethodExpression)
            .MaximumLength(500);

        validator.RuleFor(retiredDateExpression)
            .Must((instance, retiredDate) => !retiredDate.HasValue || statusAccessor(instance) is DeviceStatus.Retired or DeviceStatus.Disposed)
            .WithMessage("RetiredDate can only be set when Status is Retired or Disposed.");

        validator.RuleFor(disposalMethodExpression)
            .Must((instance, disposalMethod) => string.IsNullOrWhiteSpace(disposalMethod) || statusAccessor(instance) is DeviceStatus.Retired or DeviceStatus.Disposed)
            .WithMessage("DisposalMethod can only be set when Status is Retired or Disposed.");
    }

    public static void ApplyRequiredReferenceRules<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, Guid>> brandIdExpression,
        Expression<Func<T, Guid>> categoryIdExpression,
        Expression<Func<T, Guid>> ownerIdExpression,
        Expression<Func<T, Guid>> locationIdExpression)
    {
        ArgumentNullException.ThrowIfNull(validator);

        validator.RuleFor(brandIdExpression)
            .NotEmpty()
            .WithMessage("BrandId is required.");

        validator.RuleFor(categoryIdExpression)
            .NotEmpty()
            .WithMessage("CategoryId is required.");

        validator.RuleFor(ownerIdExpression)
            .NotEmpty()
            .WithMessage("OwnerId is required.");

        validator.RuleFor(locationIdExpression)
            .NotEmpty()
            .WithMessage("LocationId is required.");
    }

    public static void ApplyOptionalNetworkRule<T>(AbstractValidator<T> validator, Expression<Func<T, Guid?>> networkIdExpression)
    {
        ArgumentNullException.ThrowIfNull(validator);

        validator.RuleFor(networkIdExpression)
            .Must(ValidationRules.BeValidOptionalGuid)
            .WithMessage("NetworkId must be a non-empty GUID when provided.");
    }
}
