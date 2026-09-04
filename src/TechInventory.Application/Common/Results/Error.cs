using System.Collections.ObjectModel;

namespace TechInventory.Application.Common.Results;

public sealed record Error
{
    public Error(string code, string message, IReadOnlyDictionary<string, string[]>? validationErrors = null)
    {
        Code = string.IsNullOrWhiteSpace(code)
            ? throw new ArgumentException("Error code is required.", nameof(code))
            : code.Trim();

        Message = string.IsNullOrWhiteSpace(message)
            ? throw new ArgumentException("Error message is required.", nameof(message))
            : message.Trim();

        ValidationErrors = NormalizeValidationErrors(validationErrors);
    }

    public string Code { get; }

    public string Message { get; }

    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }

    public static Error Conflict(string message) => new("Conflict", message);

    public static Error NotFound(string message) => new("NotFound", message);

    /// <summary>The caller is authenticated but not permitted to perform this action (#149).</summary>
    public static Error Forbidden(string message) => new("Forbidden", message);

    /// <summary>A per-principal allowance is already fully used (#149: five active API keys).</summary>
    public static Error QuotaExceeded(string message) => new("QuotaExceeded", message);

    public static Error Validation(IReadOnlyDictionary<string, string[]> validationErrors, string message = "One or more validation failures occurred.")
        => new("Validation", message, validationErrors);

    private static IReadOnlyDictionary<string, string[]> NormalizeValidationErrors(IReadOnlyDictionary<string, string[]>? validationErrors)
    {
        var normalized = validationErrors is null
            ? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            : validationErrors
                .ToDictionary(
                    pair => string.IsNullOrWhiteSpace(pair.Key) ? "request" : pair.Key.Trim(),
                    pair => pair.Value
                        .Where(message => !string.IsNullOrWhiteSpace(message))
                        .Select(message => message.Trim())
                        .Distinct(StringComparer.Ordinal)
                        .ToArray(),
                    StringComparer.OrdinalIgnoreCase);

        return new ReadOnlyDictionary<string, string[]>(normalized);
    }
}
