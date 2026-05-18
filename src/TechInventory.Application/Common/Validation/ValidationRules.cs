using TechInventory.Domain.ValueObjects;

namespace TechInventory.Application.Common.Validation;

internal static class ValidationRules
{
    public static bool BeValidOptionalGuid(Guid? value) => !value.HasValue || value.Value != Guid.Empty;

    public static bool BeValidCurrencyCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        return TryCreateCurrency(code);
    }

    public static bool BeValidOptionalCurrencyCode(string? code)
        => string.IsNullOrWhiteSpace(code) || TryCreateCurrency(code);

    public static bool BeValidSort(string? sortBy, params string[] allowedValues)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return true;
        }

        var candidate = sortBy.Trim();
        return allowedValues.Any(allowedValue => string.Equals(allowedValue, candidate, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryCreateCurrency(string code)
    {
        try
        {
            _ = Currency.From(code);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
