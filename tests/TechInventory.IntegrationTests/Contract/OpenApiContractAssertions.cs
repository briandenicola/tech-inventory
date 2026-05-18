using System.Globalization;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;

namespace TechInventory.IntegrationTests.Contract;

internal static class OpenApiContractAssertions
{
    public static OpenApiDocument LoadDocument(string content, string sourceName)
    {
        var document = new OpenApiStringReader().Read(content, out var diagnostic);
        diagnostic.Errors.Should().BeEmpty($"OpenAPI document '{sourceName}' should parse without errors.");
        document.Should().NotBeNull($"OpenAPI document '{sourceName}' should parse successfully.");
        return document;
    }

    public static string ToCanonicalJson(OpenApiDocument document)
    {
        using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        var jsonWriter = new OpenApiJsonWriter(stringWriter);
        document.SerializeAsV3(jsonWriter);
        jsonWriter.Flush();

        var parsed = JsonNode.Parse(stringWriter.ToString());
        parsed.Should().NotBeNull();
        return Canonicalize(parsed!).ToJsonString();
    }

    public static string DescribeDrift(string expectedCanonicalJson, string actualCanonicalJson)
    {
        var expected = JsonNode.Parse(expectedCanonicalJson);
        var actual = JsonNode.Parse(actualCanonicalJson);
        expected.Should().NotBeNull();
        actual.Should().NotBeNull();

        var differences = new List<string>();
        CollectDifferences("$", expected, actual, differences);

        return differences.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, differences.Take(20));
    }

    public static void AssertResponseMatchesSchema(
        OpenApiDocument document,
        string method,
        string specPath,
        string statusCode,
        string mediaType,
        JsonNode? responseBody)
    {
        document.Paths.TryGetValue(specPath, out var pathItem).Should().BeTrue($"Spec path '{specPath}' should exist.");
        pathItem!.Operations.TryGetValue(ParseOperationType(method), out var operation).Should().BeTrue($"Operation {method} {specPath} should exist.");
        operation!.Responses.TryGetValue(statusCode, out var response).Should().BeTrue($"Response {statusCode} for {method} {specPath} should exist.");

        var media = response!.Content
            .FirstOrDefault(pair => string.Equals(pair.Key, mediaType, StringComparison.OrdinalIgnoreCase));

        media.Equals(default(KeyValuePair<string, OpenApiMediaType>)).Should().BeFalse($"Response {statusCode} for {method} {specPath} should define media type '{mediaType}'.");

        var errors = new List<string>();
        ValidateNode(responseBody, media.Value.Schema, document, "$", errors);
        errors.Should().BeEmpty(string.Join(Environment.NewLine, errors));
    }

    private static JsonNode Canonicalize(JsonNode node)
    {
        return node switch
        {
            JsonObject jsonObject => CanonicalizeObject(jsonObject),
            JsonArray jsonArray => CanonicalizeArray(jsonArray),
            _ => node.DeepClone()
        };
    }

    private static JsonObject CanonicalizeObject(JsonObject jsonObject)
    {
        var normalized = new JsonObject();
        foreach (var property in jsonObject.OrderBy(candidate => candidate.Key, StringComparer.Ordinal))
        {
            normalized[property.Key] = property.Value is null ? null : Canonicalize(property.Value);
        }

        return normalized;
    }

    private static JsonArray CanonicalizeArray(JsonArray jsonArray)
    {
        var normalizedItems = jsonArray
            .Select(item => item is null ? null : Canonicalize(item))
            .OrderBy(item => item?.ToJsonString() ?? "null", StringComparer.Ordinal)
            .ToList();

        var normalized = new JsonArray();
        foreach (var item in normalizedItems)
        {
            normalized.Add(item);
        }

        return normalized;
    }

    private static void CollectDifferences(string path, JsonNode? expected, JsonNode? actual, List<string> differences)
    {
        if (differences.Count >= 20)
        {
            return;
        }

        if (expected is null || actual is null)
        {
            if (expected is null && actual is null)
            {
                return;
            }

            differences.Add($"{path}: expected {(expected is null ? "null" : expected.ToJsonString())}, actual {(actual is null ? "null" : actual.ToJsonString())}");
            return;
        }

        if (expected is JsonValue || actual is JsonValue)
        {
            if (!string.Equals(expected.ToJsonString(), actual.ToJsonString(), StringComparison.Ordinal))
            {
                differences.Add($"{path}: expected {expected.ToJsonString()}, actual {actual.ToJsonString()}");
            }

            return;
        }

        if (expected is JsonObject expectedObject && actual is JsonObject actualObject)
        {
            var keys = expectedObject.Select(property => property.Key)
                .Union(actualObject.Select(property => property.Key), StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal);

            foreach (var key in keys)
            {
                expectedObject.TryGetPropertyValue(key, out var expectedValue);
                actualObject.TryGetPropertyValue(key, out var actualValue);
                CollectDifferences($"{path}.{key}", expectedValue, actualValue, differences);
            }

            return;
        }

        if (expected is JsonArray expectedArray && actual is JsonArray actualArray)
        {
            if (expectedArray.Count != actualArray.Count)
            {
                differences.Add($"{path}: expected array length {expectedArray.Count}, actual {actualArray.Count}");
            }

            foreach (var index in Enumerable.Range(0, Math.Min(expectedArray.Count, actualArray.Count)))
            {
                CollectDifferences($"{path}[{index}]", expectedArray[index], actualArray[index], differences);
            }

            return;
        }

        differences.Add($"{path}: expected node type {expected.GetType().Name}, actual {actual.GetType().Name}");
    }

    private static void ValidateNode(JsonNode? node, OpenApiSchema schema, OpenApiDocument document, string path, List<string> errors)
    {
        schema = ResolveSchema(schema, document);

        if (schema.AllOf.Count > 0)
        {
            foreach (var component in schema.AllOf)
            {
                ValidateNode(node, component, document, path, errors);
            }

            return;
        }

        if (schema.OneOf.Count > 0)
        {
            if (!schema.OneOf.Any(option => IsValid(node, option, document, path)))
            {
                errors.Add($"{path}: value did not match any allowed schema option.");
            }

            return;
        }

        if (schema.AnyOf.Count > 0)
        {
            if (!schema.AnyOf.Any(option => IsValid(node, option, document, path)))
            {
                errors.Add($"{path}: value did not match any acceptable schema option.");
            }

            return;
        }

        if (node is null)
        {
            if (!schema.Nullable && !string.IsNullOrWhiteSpace(GetEffectiveType(schema)))
            {
                errors.Add($"{path}: null is not allowed by schema.");
            }

            return;
        }

        switch (GetEffectiveType(schema))
        {
            case "object":
                ValidateObject(node, schema, document, path, errors);
                break;
            case "array":
                ValidateArray(node, schema, document, path, errors);
                break;
            case "integer":
                ValidateInteger(node, path, errors);
                break;
            case "number":
                ValidateNumber(node, path, errors);
                break;
            case "boolean":
                ValidateBoolean(node, path, errors);
                break;
            case "string":
                ValidateString(node, schema, path, errors);
                break;
            default:
                break;
        }
    }

    private static void ValidateObject(JsonNode node, OpenApiSchema schema, OpenApiDocument document, string path, List<string> errors)
    {
        if (node is not JsonObject jsonObject)
        {
            errors.Add($"{path}: expected object but got {node.GetType().Name}.");
            return;
        }

        foreach (var requiredProperty in schema.Required)
        {
            if (!TryGetProperty(jsonObject, requiredProperty, out _))
            {
                errors.Add($"{path}.{requiredProperty}: required property missing.");
            }
        }

        foreach (var property in schema.Properties)
        {
            if (TryGetProperty(jsonObject, property.Key, out var value))
            {
                ValidateNode(value, property.Value, document, $"{path}.{property.Key}", errors);
            }
        }

        foreach (var property in jsonObject)
        {
            if (schema.Properties.Keys.Any(key => string.Equals(key, property.Key, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (schema.AdditionalProperties is not null)
            {
                ValidateNode(property.Value, schema.AdditionalProperties, document, $"{path}.{property.Key}", errors);
                continue;
            }

            if (!schema.AdditionalPropertiesAllowed)
            {
                errors.Add($"{path}.{property.Key}: additional property not allowed by schema.");
            }
        }
    }

    private static void ValidateArray(JsonNode node, OpenApiSchema schema, OpenApiDocument document, string path, List<string> errors)
    {
        if (node is not JsonArray jsonArray)
        {
            errors.Add($"{path}: expected array but got {node.GetType().Name}.");
            return;
        }

        foreach (var (item, index) in jsonArray.Select((value, index) => (value, index)))
        {
            ValidateNode(item, schema.Items, document, $"{path}[{index}]", errors);
        }
    }

    private static void ValidateInteger(JsonNode node, string path, List<string> errors)
    {
        if (node is not JsonValue value || !value.TryGetValue<long>(out _))
        {
            errors.Add($"{path}: expected integer value.");
        }
    }

    private static void ValidateNumber(JsonNode node, string path, List<string> errors)
    {
        if (node is not JsonValue value || !value.TryGetValue<decimal>(out _))
        {
            errors.Add($"{path}: expected numeric value.");
        }
    }

    private static void ValidateBoolean(JsonNode node, string path, List<string> errors)
    {
        if (node is not JsonValue value || !value.TryGetValue<bool>(out _))
        {
            errors.Add($"{path}: expected boolean value.");
        }
    }

    private static void ValidateString(JsonNode node, OpenApiSchema schema, string path, List<string> errors)
    {
        if (node is not JsonValue value || !value.TryGetValue<string>(out var stringValue))
        {
            errors.Add($"{path}: expected string value.");
            return;
        }

        if (schema.Enum.Count > 0)
        {
            var allowed = schema.Enum.Select(ToScalarString).ToArray();
            if (!allowed.Contains(stringValue, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"{path}: value '{stringValue}' not present in enum [{string.Join(", ", allowed)}].");
            }
        }

        if (string.Equals(schema.Format, "uuid", StringComparison.OrdinalIgnoreCase) && !Guid.TryParse(stringValue, out _))
        {
            errors.Add($"{path}: expected UUID string.");
        }

        if (string.Equals(schema.Format, "date", StringComparison.OrdinalIgnoreCase) && !DateOnly.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            errors.Add($"{path}: expected date string.");
        }

        if (string.Equals(schema.Format, "date-time", StringComparison.OrdinalIgnoreCase) && !DateTimeOffset.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
        {
            errors.Add($"{path}: expected date-time string.");
        }
    }

    private static bool IsValid(JsonNode? node, OpenApiSchema schema, OpenApiDocument document, string path)
    {
        var errors = new List<string>();
        ValidateNode(node, schema, document, path, errors);
        return errors.Count == 0;
    }

    private static string GetEffectiveType(OpenApiSchema schema)
    {
        if (!string.IsNullOrWhiteSpace(schema.Type))
        {
            return schema.Type;
        }

        if (schema.Properties.Count > 0 || schema.Required.Count > 0)
        {
            return "object";
        }

        if (schema.Items is not null)
        {
            return "array";
        }

        if (schema.Enum.Count > 0)
        {
            return "string";
        }

        return string.Empty;
    }

    private static OpenApiSchema ResolveSchema(OpenApiSchema schema, OpenApiDocument document)
    {
        if (schema.Reference?.Id is not { Length: > 0 } referenceId)
        {
            return schema;
        }

        return document.Components.Schemas.TryGetValue(referenceId, out var referenced)
            ? referenced
            : schema;
    }

    private static OperationType ParseOperationType(string method)
        => method.ToUpperInvariant() switch
        {
            "GET" => OperationType.Get,
            "POST" => OperationType.Post,
            "PUT" => OperationType.Put,
            "PATCH" => OperationType.Patch,
            "DELETE" => OperationType.Delete,
            _ => throw new InvalidOperationException($"Unsupported HTTP method '{method}'.")
        };

    private static bool TryGetProperty(JsonObject jsonObject, string propertyName, out JsonNode? value)
    {
        foreach (var property in jsonObject)
        {
            if (string.Equals(property.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static string ToScalarString(IOpenApiAny value)
        => value switch
        {
            OpenApiString openApiString => openApiString.Value ?? string.Empty,
            OpenApiInteger openApiInteger => openApiInteger.Value.ToString(CultureInfo.InvariantCulture),
            OpenApiLong openApiLong => openApiLong.Value.ToString(CultureInfo.InvariantCulture),
            OpenApiBoolean openApiBoolean => openApiBoolean.Value.ToString(),
            OpenApiFloat openApiFloat => openApiFloat.Value.ToString(CultureInfo.InvariantCulture),
            OpenApiDouble openApiDouble => openApiDouble.Value.ToString(CultureInfo.InvariantCulture),
            OpenApiDate openApiDate => openApiDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            OpenApiDateTime openApiDateTime => openApiDateTime.Value.ToString("O", CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
}

public sealed class ApiMarker;
