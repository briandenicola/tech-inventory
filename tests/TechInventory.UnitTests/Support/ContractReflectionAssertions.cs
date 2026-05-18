using System.Reflection;
using FluentAssertions;
using NSubstitute;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using Xunit.Sdk;

namespace TechInventory.UnitTests.Support;

internal static class ContractReflectionAssertions
{
    public static readonly AuditContractSamples AuditSamples = new(
        Actor: "apone",
        ActorIntId: 7,
        ActorGuid: Guid.Parse("11111111-1111-1111-1111-111111111111"),
        EntityType: "Device",
        EntityIntId: 42,
        EntityGuid: Guid.Parse("22222222-2222-2222-2222-222222222222"),
        Timestamp: new DateTimeOffset(2025, 5, 18, 18, 30, 0, TimeSpan.Zero),
        BeforeJson: "{\"status\":\"before\"}",
        AfterJson: "{\"status\":\"after\"}",
        IpAddress: "127.0.0.1",
        CorrelationId: "corr-apone-001");

    private static readonly Assembly DomainAssembly = typeof(Brand).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(TechInventory.Application.Abstractions.Repositories.IAuditEventRepository).Assembly;

    public static Type RequireDomainType(string fullName, string skipReason)
        => DomainAssembly.GetType(fullName) ?? throw new XunitException(skipReason);

    public static Type RequireApplicationType(string fullName, string skipReason)
        => ApplicationAssembly.GetType(fullName) ?? throw new XunitException(skipReason);

    public static ConstructorInfo RequirePrimaryConstructor(Type type)
        => type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
               .OrderByDescending(constructor => constructor.GetParameters().Length)
               .FirstOrDefault()
           ?? throw new XunitException($"{type.FullName} should expose a public constructor.");

    public static object CreateAuditEvent(Type auditEventType)
        => CreateInstance(RequirePrimaryConstructor(auditEventType), AuditSamples);

    public static object CreateInstance(ConstructorInfo constructor, AuditContractSamples samples, params (string ParameterName, object? Value)[] overrides)
    {
        var overrideMap = overrides.ToDictionary(
            entry => Normalize(entry.ParameterName),
            entry => entry.Value,
            StringComparer.OrdinalIgnoreCase);

        var arguments = constructor
            .GetParameters()
            .Select(parameter => overrideMap.TryGetValue(Normalize(parameter.Name), out var value)
                ? value
                : CreateValue(parameter.ParameterType, parameter.Name, samples))
            .ToArray();

        return constructor.Invoke(arguments);
    }

    public static object CreateSubstitute(Type interfaceType)
    {
        interfaceType.IsInterface.Should().BeTrue($"{interfaceType.FullName} should be an interface.");
        return Substitute.For(new[] { interfaceType }, Array.Empty<object>());
    }

    public static PropertyInfo RequireProperty(Type type, params string[] candidateNames)
        => TryGetProperty(type, candidateNames)
           ?? throw new XunitException($"{type.FullName} should expose one of: {string.Join(", ", candidateNames)}.");

    public static PropertyInfo? TryGetProperty(Type type, params string[] candidateNames)
        => candidateNames
            .Select(name => type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase))
            .FirstOrDefault(property => property is not null);

    public static object? ExpectedAuditValue(PropertyInfo property)
    {
        var normalizedName = Normalize(property.Name);
        return normalizedName switch
        {
            var name when name.Contains("ACTOR") || name.Contains("USER") => CoerceIdentifier(property.PropertyType, actor: true),
            var name when name.Contains("ACTION") => CoerceAction(property.PropertyType),
            var name when name.Contains("BEFORE") => AuditSamples.BeforeJson,
            var name when name.Contains("AFTER") => AuditSamples.AfterJson,
            var name when name.Contains("TIMESTAMP") || name.Contains("OCCURRED") => CoerceTimestamp(property.PropertyType, AuditSamples.Timestamp),
            _ => throw new XunitException($"No audit sample value is defined for property {property.Name}.")
        };
    }

    public static object?[] CreateConstructorArgumentsWithOverride(ConstructorInfo constructor, string parameterName, object? value)
        => constructor
            .GetParameters()
            .Select(parameter => string.Equals(Normalize(parameter.Name), Normalize(parameterName), StringComparison.OrdinalIgnoreCase)
                ? value
                : CreateValue(parameter.ParameterType, parameter.Name, AuditSamples))
            .ToArray();

    public static bool ReturnsAllowedRepositoryShape(Type returnType)
    {
        if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(Task<>))
        {
            return false;
        }

        var innerType = returnType.GetGenericArguments()[0];
        return IsResultType(innerType) || IsReadOnlyListType(innerType) || IsPagedResultType(innerType);
    }

    public static bool HasCancellationToken(MethodInfo method)
        => method.GetParameters().Any(parameter => parameter.ParameterType == typeof(CancellationToken));

    public static bool ContainsQueryable(Type type)
    {
        if (type == typeof(IQueryable))
        {
            return true;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>))
        {
            return true;
        }

        if (type.IsGenericType)
        {
            return type.GetGenericArguments().Any(ContainsQueryable);
        }

        return false;
    }

    public static bool IsUtc(object value)
        => value switch
        {
            DateTimeOffset dto => dto.Offset == TimeSpan.Zero,
            DateTime dt => dt.Kind == DateTimeKind.Utc,
            _ => throw new XunitException($"Unsupported timestamp type: {value.GetType().FullName}")
        };

    public static DateTimeOffset ToDateTimeOffset(object value)
        => value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(dt, TimeSpan.Zero),
            _ => throw new XunitException($"Unsupported timestamp type: {value.GetType().FullName}")
        };

    public static Exception Unwrap(Exception exception)
        => exception is TargetInvocationException { InnerException: not null } invocationException
            ? invocationException.InnerException!
            : exception;

    public static string Normalize(string? value)
        => string.Concat((value ?? string.Empty).Where(character => character != '_' && character != '-')).ToUpperInvariant();

    public static ParameterInfo RequireStringParameter(ConstructorInfo constructor, params string[] candidateNames)
        => constructor
               .GetParameters()
               .FirstOrDefault(parameter => parameter.ParameterType == typeof(string) && candidateNames.Contains(Normalize(parameter.Name), StringComparer.OrdinalIgnoreCase))
           ?? throw new XunitException($"{constructor.DeclaringType?.FullName} should expose a string constructor parameter matching one of: {string.Join(", ", candidateNames)}.");

    private static object? CreateValue(Type parameterType, string? parameterName, AuditContractSamples samples)
    {
        var normalizedName = Normalize(parameterName);
        var nullableUnderlyingType = Nullable.GetUnderlyingType(parameterType);
        if (nullableUnderlyingType is not null)
        {
            return CreateValue(nullableUnderlyingType, parameterName, samples);
        }

        if (parameterType == typeof(string))
        {
            return CreateStringValue(normalizedName, samples);
        }

        if (parameterType == typeof(DateTimeOffset))
        {
            return samples.Timestamp;
        }

        if (parameterType == typeof(DateTime))
        {
            return samples.Timestamp.UtcDateTime;
        }

        if (parameterType == typeof(Guid) || parameterType == typeof(int) || parameterType == typeof(long))
        {
            return CoerceIdentifier(parameterType, actor: normalizedName.Contains("ACTOR") || normalizedName.Contains("USER"));
        }

        if (parameterType.IsEnum)
        {
            return CoerceAction(parameterType);
        }

        if (parameterType == typeof(bool))
        {
            return true;
        }

        throw new XunitException($"Unsupported constructor parameter type for contract test data: {parameterType.FullName} ({parameterName}).");
    }

    private static string CreateStringValue(string normalizedName, AuditContractSamples samples)
        => normalizedName switch
        {
            var name when name.Contains("ACTOR") || name.Contains("USER") => samples.Actor,
            var name when name.Contains("ENTITYTYPE") => samples.EntityType,
            var name when name.Contains("ACTION") => "Updated",
            var name when name.Contains("BEFORE") => samples.BeforeJson,
            var name when name.Contains("AFTER") => samples.AfterJson,
            var name when name.Contains("CORRELATION") => samples.CorrelationId,
            var name when name.Contains("IP") => samples.IpAddress,
            var name when name.Contains("ENTITY") && name.Contains("ID") => samples.EntityGuid.ToString(),
            var name when name.EndsWith("ID") => samples.EntityGuid.ToString(),
            _ => "value"
        };

    private static object CoerceAction(Type targetType)
    {
        if (targetType == typeof(string))
        {
            return "Updated";
        }

        var actionName = Enum.GetNames(targetType).FirstOrDefault(name => string.Equals(name, "Updated", StringComparison.OrdinalIgnoreCase))
            ?? Enum.GetNames(targetType).FirstOrDefault()
            ?? throw new XunitException($"{targetType.FullName} should define at least one enum value.");

        return Enum.Parse(targetType, actionName, ignoreCase: true);
    }

    private static object CoerceIdentifier(Type targetType, bool actor)
    {
        if (targetType == typeof(string))
        {
            return actor ? AuditSamples.Actor : AuditSamples.EntityGuid.ToString();
        }

        if (targetType == typeof(int))
        {
            return actor ? AuditSamples.ActorIntId : AuditSamples.EntityIntId;
        }

        if (targetType == typeof(long))
        {
            return actor ? AuditSamples.ActorIntId : AuditSamples.EntityIntId;
        }

        if (targetType == typeof(Guid))
        {
            return actor ? AuditSamples.ActorGuid : AuditSamples.EntityGuid;
        }

        throw new XunitException($"Unsupported identifier type: {targetType.FullName}.");
    }

    private static object CoerceTimestamp(Type targetType, DateTimeOffset timestamp)
        => targetType == typeof(DateTimeOffset)
            ? timestamp
            : targetType == typeof(DateTime)
                ? timestamp.UtcDateTime
                : throw new XunitException($"Unsupported timestamp property type: {targetType.FullName}");

    private static bool IsResultType(Type type)
        => type == typeof(Result) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>));

    private static bool IsReadOnlyListType(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>);

    private static bool IsPagedResultType(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PagedResult<>);
}

internal sealed record AuditContractSamples(
    string Actor,
    int ActorIntId,
    Guid ActorGuid,
    string EntityType,
    int EntityIntId,
    Guid EntityGuid,
    DateTimeOffset Timestamp,
    string BeforeJson,
    string AfterJson,
    string IpAddress,
    string CorrelationId);