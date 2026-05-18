namespace TechInventory.Domain.Primitives;

public static class Guard
{
    public static Guid AgainstDefault(Guid value, string paramName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException($"{paramName} cannot be an empty GUID.", paramName);
        }

        return value;
    }

    public static Guid? AgainstOptionalDefault(Guid? value, string paramName)
    {
        if (value.HasValue && value.Value == Guid.Empty)
        {
            throw new ArgumentException($"{paramName} cannot be an empty GUID.", paramName);
        }

        return value;
    }

    public static string AgainstNullOrWhiteSpace(string? value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} is required.", paramName);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, $"{paramName} cannot exceed {maxLength} characters.");
        }

        return trimmed;
    }

    public static string? AgainstMaxLength(string? value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, $"{paramName} cannot exceed {maxLength} characters.");
        }

        return trimmed;
    }

    public static decimal? AgainstNegative(decimal? value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, $"{paramName} cannot be negative.");
        }

        return value;
    }
}
