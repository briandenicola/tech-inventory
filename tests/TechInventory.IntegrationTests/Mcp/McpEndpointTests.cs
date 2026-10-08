using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using ModelContextProtocol.Client;
using TechInventory.Api.Mcp;
using TechInventory.Application.Reports;
using TechInventory.Domain.Enums;
using TechInventory.IntegrationTests.ApiKeys;
using static TechInventory.IntegrationTests.ApiKeys.ApiKeyTestSupport;

namespace TechInventory.IntegrationTests.Mcp;

[Collection("Mcp")]
public sealed class McpEndpointTests(McpTestHostFactory factory)
    : IClassFixture<McpTestHostFactory>
{
    private const string Username = "mcp-admin";
    private const string Password = "Str0ng!TestPassword";

    [Fact]
    public async Task ReadKey_CanDiscoverOnlyApprovedReadOnlyTools()
    {
        var key = await CreateReadKeyAsync();
        using var httpClient = factory.CreateClient();
        UseApiKey(httpClient, key);

        await using var mcpClient = await CreateMcpClientAsync(httpClient);
        var tools = await mcpClient.ListToolsAsync();

        tools.Select(tool => tool.Name).Should().BeEquivalentTo(
        [
            "search_devices",
            "get_device",
            "list_reference_data",
            "inventory_summary",
            "warranty_report",
            "spending_report",
            "era_report",
            "timeline_report",
        ]);
        tools.Should().OnlyContain(tool =>
            tool.ProtocolTool.Annotations != null
            && tool.ProtocolTool.Annotations.ReadOnlyHint == true
            && tool.ProtocolTool.Annotations.DestructiveHint == false);
    }

    [Fact]
    public async Task ReadKey_CanCallSummaryAndReferenceTools()
    {
        var key = await CreateReadKeyAsync();
        using var httpClient = factory.CreateClient();
        UseApiKey(httpClient, key);

        await using var mcpClient = await CreateMcpClientAsync(httpClient);

        var summary = await mcpClient.CallToolAsync("inventory_summary");
        summary.IsError.Should().NotBeTrue();
        summary.StructuredContent.Should().NotBeNull();
        summary.StructuredContent!.Value.GetRawText().Should().Contain("Untrusted inventory data");

        var references = await mcpClient.CallToolAsync(
            "list_reference_data",
            new Dictionary<string, object?> { ["type"] = "brands" });
        references.IsError.Should().NotBeTrue();
        references.StructuredContent.Should().NotBeNull();

        var eras = await mcpClient.CallToolAsync("era_report");
        eras.IsError.Should().NotBeTrue();
        eras.StructuredContent.Should().NotBeNull();
        var appliedCategoryId = eras.StructuredContent!.Value
            .GetProperty("data")
            .GetProperty("appliedCategoryId");
        appliedCategoryId.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task PublishedOutputSchemas_AcceptNullableFieldsEmittedAsNull()
    {
        var key = await CreateReadKeyAsync();
        using var httpClient = factory.CreateClient();
        UseApiKey(httpClient, key);

        await using var mcpClient = await CreateMcpClientAsync(httpClient);
        var tools = (await mcpClient.ListToolsAsync()).ToDictionary(tool => tool.Name, StringComparer.Ordinal);
        var date = new DateOnly(2026, 10, 7);
        var device = new DeviceMcpResponse(
            Guid.NewGuid(),
            "Nullable device",
            null,
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            "USD",
            "Active",
            null,
            null,
            null,
            null,
            null);
        var examples = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["search_devices"] = new UntrustedDataEnvelope<McpPage<DeviceMcpResponse>>(
                "test",
                new McpPage<DeviceMcpResponse>([device], 1, 1, 25)),
            ["get_device"] = new UntrustedDataEnvelope<DeviceMcpResponse>("test", device),
            ["list_reference_data"] = new UntrustedDataEnvelope<ReferenceDataMcpResponse>(
                "test",
                new ReferenceDataMcpResponse(
                    "brands",
                    [new ReferenceItemMcpResponse(Guid.NewGuid(), "Tatybo", null)])),
            ["inventory_summary"] = new UntrustedDataEnvelope<SummaryReportResponse>(
                "test",
                new SummaryReportResponse(0, 0, [], [], [])),
            ["warranty_report"] = new UntrustedDataEnvelope<WarrantyMcpResponse>(
                "test",
                new WarrantyMcpResponse(
                    date,
                    365,
                    [new WarrantyReportItem("Nullable warranty device", null, null, date, 0)],
                    1,
                    false)),
            ["spending_report"] = new UntrustedDataEnvelope<SpendingMcpResponse>(
                "test",
                new SpendingMcpResponse(SpendingGroupBy.Month, null, null, [], 0, false)),
            ["era_report"] = new UntrustedDataEnvelope<EraReportResponse>(
                "test",
                new EraReportResponse([], date, null)),
            ["timeline_report"] = new UntrustedDataEnvelope<TimelineMcpResponse>(
                "test",
                new TimelineMcpResponse(
                    [new TimelineReportEntry("Nullable timeline device", null, date, null, "Other", 0)],
                    date,
                    TimelineGroupBy.Category,
                    null,
                    1,
                    false)),
        };
        var serializerOptions = McpJsonSerializerOptions.Create();

        foreach (var (toolName, example) in examples)
        {
            var schema = tools[toolName].ProtocolTool.OutputSchema;
            schema.Should().NotBeNull($"{toolName} publishes structured content");
            var payload = JsonSerializer.SerializeToElement(example, example.GetType(), serializerOptions);

            ValidateRequiredProperties(schema!.Value, payload, toolName);
        }
    }

    [Fact]
    public async Task WarrantyReport_DaysArgumentIsHonored()
    {
        var key = await CreateReadKeyAsync();
        using var httpClient = factory.CreateClient();
        UseApiKey(httpClient, key);

        await using var mcpClient = await CreateMcpClientAsync(httpClient);
        var warrantyTool = (await mcpClient.ListToolsAsync()).Single(tool => tool.Name == "warranty_report");
        var inputProperties = warrantyTool.ProtocolTool.InputSchema.GetProperty("properties");
        inputProperties.TryGetProperty("days", out _).Should().BeTrue();
        inputProperties.TryGetProperty("expiringWithinDays", out _).Should().BeFalse();
        var result = await mcpClient.CallToolAsync(
            "warranty_report",
            new Dictionary<string, object?> { ["days"] = 365 });

        result.IsError.Should().NotBeTrue();
        result.StructuredContent.Should().NotBeNull();
        result.StructuredContent!.Value
            .GetProperty("data")
            .GetProperty("expiringWithinDays")
            .GetInt32()
            .Should()
            .Be(365);
    }

    [Fact]
    public async Task MissingCredential_IsRejected()
    {
        await EnableMcpAsync();
        using var httpClient = factory.CreateClient();

        var action = async () =>
        {
            await using var client = await CreateMcpClientAsync(httpClient);
        };

        var exception = await action.Should().ThrowAsync<HttpRequestException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InvalidApiKey_IsRejected()
    {
        await EnableMcpAsync();
        using var httpClient = factory.CreateClient();
        UseApiKey(httpClient, "bm90LWEtcmVhbC1zZWxlY3Rvcg.bm90LWEtcmVhbC1zZWNyZXQ");

        var action = async () =>
        {
            await using var client = await CreateMcpClientAsync(httpClient);
        };

        var exception = await action.Should().ThrowAsync<HttpRequestException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokedApiKey_IsRejected()
    {
        var created = await CreateApiKeyAsync("inventory.read", "MCP revoked test");

        using var adminClient = factory.CreateClient();
        UseBearer(adminClient, await LoginAsync(adminClient, Username, Password));
        (await adminClient.DeleteAsync($"/api/v1/api-keys/{created.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var keyClient = factory.CreateClient();
        UseApiKey(keyClient, created.Key);

        var action = async () =>
        {
            await using var client = await CreateMcpClientAsync(keyClient);
        };

        var exception = await action.Should().ThrowAsync<HttpRequestException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WriteScopedKey_CanUseReadOnlyTools()
    {
        var created = await CreateApiKeyAsync("inventory.write", "MCP write-scope test");
        using var httpClient = factory.CreateClient();
        UseApiKey(httpClient, created.Key);

        await using var mcpClient = await CreateMcpClientAsync(httpClient);
        var tools = await mcpClient.ListToolsAsync();

        tools.Should().HaveCount(8);
        tools.Should().OnlyContain(tool =>
            tool.ProtocolTool.Annotations != null
            && tool.ProtocolTool.Annotations.ReadOnlyHint == true);
    }

    [Fact]
    public async Task BearerCredential_IsRejected()
    {
        using var httpClient = await EnableMcpAsync();

        var action = async () =>
        {
            await using var client = await CreateMcpClientAsync(httpClient);
        };

        var exception = await action.Should().ThrowAsync<HttpRequestException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task NonPostMethods_AreDeniedBeforeRouting()
    {
        var key = await CreateReadKeyAsync();
        using var httpClient = factory.CreateClient();
        UseApiKey(httpClient, key);

        (await httpClient.GetAsync("/api/mcp")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await httpClient.DeleteAsync("/api/mcp")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OversizedRequest_IsRejected()
    {
        using var adminClient = await EnableMcpAsync();
        using var httpClient = factory.CreateClient();
        using var content = new StringContent(
            new string('x', 131073),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.PostAsync("/api/mcp", content);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public void DeviceContract_DoesNotExposeSensitiveOrAuditFields()
    {
        var properties = typeof(DeviceMcpResponse)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        properties.Should().NotContain(
        [
            "SerialNumber",
            "IpAddress",
            "MacAddress",
            "Notes",
            "ProductUrl",
            "CreatedAt",
            "CreatedBy",
            "ModifiedAt",
            "ModifiedBy",
        ]);
    }

    private async Task<string> CreateReadKeyAsync()
        => (await CreateApiKeyAsync("inventory.read", "Hermes Agent")).Key;

    private async Task<CreatedKeyDto> CreateApiKeyAsync(string scope, string name)
    {
        using var adminClient = await EnableMcpAsync();
        var response = await adminClient.PostAsJsonAsync(
            "/api/v1/api-keys",
            new { name, scope, expiresInDays = (int?)null },
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<CreatedKeyDto>(JsonOptions))!;
    }

    private async Task<HttpClient> EnableMcpAsync()
    {
        await ResetDatabaseAsync(factory);
        await EnsureHouseholdAsync(factory);
        await ResetAndSeedLocalUserAsync(factory, OwnerRole.Admin, Username, Password);

        var adminClient = factory.CreateClient();
        UseBearer(adminClient, await LoginAsync(adminClient, Username, Password));
        var response = await adminClient.PutAsJsonAsync(
            "/api/v1/settings/mcp",
            new { enabled = true },
            JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return adminClient;
    }

    private static Task<McpClient> CreateMcpClientAsync(HttpClient httpClient)
        => McpClient.CreateAsync(
            new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = new Uri(httpClient.BaseAddress!, "/api/mcp"),
                    Name = "Tech Inventory integration test",
                    TransportMode = HttpTransportMode.StreamableHttp,
                },
                httpClient));

    private static void ValidateRequiredProperties(JsonElement schema, JsonElement instance, string path)
    {
        if (schema.TryGetProperty("required", out var required))
        {
            instance.ValueKind.Should().Be(JsonValueKind.Object, $"{path} is described as an object");
            foreach (var requiredProperty in required.EnumerateArray())
            {
                var propertyName = requiredProperty.GetString()!;
                instance.TryGetProperty(propertyName, out _)
                    .Should()
                    .BeTrue($"{path}.{propertyName} is required by the published schema");
            }
        }

        if (instance.ValueKind == JsonValueKind.Object
            && schema.TryGetProperty("properties", out var properties))
        {
            foreach (var propertySchema in properties.EnumerateObject())
            {
                if (instance.TryGetProperty(propertySchema.Name, out var propertyValue)
                    && propertyValue.ValueKind is not JsonValueKind.Null)
                {
                    ValidateRequiredProperties(
                        propertySchema.Value,
                        propertyValue,
                        $"{path}.{propertySchema.Name}");
                }
            }
        }

        if (instance.ValueKind == JsonValueKind.Array
            && schema.TryGetProperty("items", out var itemSchema))
        {
            var index = 0;
            foreach (var item in instance.EnumerateArray())
            {
                ValidateRequiredProperties(itemSchema, item, $"{path}[{index}]");
                index++;
            }
        }
    }
}

public sealed class McpRateLimitTests(McpThrottledTestHostFactory factory)
    : IClassFixture<McpThrottledTestHostFactory>
{
    private const string Username = "mcp-rate-admin";
    private const string Password = "Str0ng!TestPassword";

    [Fact]
    public async Task Endpoint_UsesPerApiKeyRateLimit()
    {
        await ResetDatabaseAsync(factory);
        await EnsureHouseholdAsync(factory);
        await ResetAndSeedLocalUserAsync(factory, OwnerRole.Admin, Username, Password);

        using var adminClient = factory.CreateClient();
        UseBearer(adminClient, await LoginAsync(adminClient, Username, Password));
        var settingsResponse = await adminClient.PutAsJsonAsync(
            "/api/v1/settings/mcp",
            new { enabled = true },
            JsonOptions);
        settingsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createResponse = await adminClient.PostAsJsonAsync(
            "/api/v1/api-keys",
            new { name = "MCP rate test", scope = "inventory.read", expiresInDays = (int?)null },
            JsonOptions);
        var key = (await createResponse.Content.ReadFromJsonAsync<CreatedKeyDto>(JsonOptions))!.Key;

        using var keyClient = factory.CreateClient();
        UseApiKey(keyClient, key);

        HttpResponseMessage? response = null;
        for (var index = 0; index <= ApiKeyTestHostFactory.TestPermitLimit; index++)
        {
            response?.Dispose();
            response = await keyClient.PostAsJsonAsync("/api/mcp", new { });
        }

        using (response)
        {
            response!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter.Should().NotBeNull();
        }
    }
}

public sealed class McpDisabledEndpointTests(ApiKeyUnthrottledTestHostFactory factory)
    : IClassFixture<ApiKeyUnthrottledTestHostFactory>
{
    [Fact]
    public async Task Endpoint_IsDisabledByDefault()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/mcp",
            new { jsonrpc = "2.0", id = 1, method = "initialize", @params = new { } });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
