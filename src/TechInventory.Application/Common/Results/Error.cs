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
