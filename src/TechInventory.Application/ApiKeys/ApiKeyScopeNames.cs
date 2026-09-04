using TechInventory.Domain.Enums;

namespace TechInventory.Application.ApiKeys;

/// <summary>
/// Wire names for <see cref="ApiKeyScope"/>.
/// </summary>
/// <remarks>
/// The API contract uses <c>inventory.read</c> / <c>inventory.write</c>, not the enum
/// member names. Responses carry these strings rather than the enum so the serializer
/// cannot quietly emit <c>Read</c> and break clients — an integration test caught
/// exactly that.
/// </remarks>
public static class ApiKeyScopeNames
{
    public const string Read = "inventory.read";
    public const string Write = "inventory.write";

    public static string ToWireValue(ApiKeyScope scope) => scope switch
    {
        ApiKeyScope.Read => Read,
        ApiKeyScope.Write => Write,
        _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unknown API key scope."),
    };

    /// <summary>
    /// Parses a wire value. Unknown input maps to a sentinel that fails enum validation,
    /// so a typo yields a 400 naming the field rather than defaulting to a scope the
    /// caller never asked for.
    /// </summary>
    public static ApiKeyScope FromWireValue(string? value) => value switch
    {
        Read => ApiKeyScope.Read,
        Write => ApiKeyScope.Write,
        _ => (ApiKeyScope)0,
    };
}
