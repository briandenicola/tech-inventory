using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using ModelContextProtocol.Client;
using TechInventory.Api.Mcp;
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
