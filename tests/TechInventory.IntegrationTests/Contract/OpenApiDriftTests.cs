using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using FluentAssertions;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.IntegrationTests.Contract;

public sealed class OpenApiDriftTests(IntegrationTestFactory<ApiMarker> factory)
    : Controllers.ControllerTestBase<ApiMarker>(factory), IClassFixture<IntegrationTestFactory<ApiMarker>>
{
    private const string WaitingOnExportResponseSchemaSkip = "OpenAPI 200 response for /api/v1/exports/devices does not declare a body schema yet.";

    [Fact]
    public async Task RuntimeOpenApi_WhenComparedToCommittedSpec_HasNoStructuralDrift()
    {
        using var client = CreateClient();

        var runtimeResponse = await client.GetAsync("/openapi/v1.json");
        runtimeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var runtimeDocument = OpenApiContractAssertions.LoadDocument(await runtimeResponse.Content.ReadAsStringAsync(), "runtime /openapi/v1.json");
        var committedDocument = OpenApiContractAssertions.LoadDocument(await File.ReadAllTextAsync(ResolveCommittedOpenApiPath()), "committed openapi.yaml");

        var runtimeCanonical = OpenApiContractAssertions.ToCanonicalJson(runtimeDocument);
        var committedCanonical = OpenApiContractAssertions.ToCanonicalJson(committedDocument);
        runtimeCanonical.Should().Be(committedCanonical, OpenApiContractAssertions.DescribeDrift(committedCanonical, runtimeCanonical));
    }

    // B4 (Ripley T102 second review): every mutation gated by
    // AuthorizationPolicies.AdminOrMember must document a 403 Forbidden
    // response in the committed OpenAPI contract, not just carry the
    // `[Authorize]` attribute at runtime. This is a parameterized contract
    // assertion (not 26 repetitive test methods) covering every reference
    // entity (Brands/Categories/Locations/Networks/Tags/Owners)
    // Create/Update/Delete action, the 7 AdminOrMember-gated Devices
    // actions, and the Imports commit action — the same 26 operations
    // that gained `[ProducesResponseType(StatusCodes.Status403Forbidden)]`
    // in this revision. Deliberately independent of
    // `RuntimeOpenApi_WhenComparedToCommittedSpec_HasNoStructuralDrift`
    // above: that test only proves the committed spec matches whatever the
    // API emits, so a mutation missing its 403 declaration entirely would
    // still pass it. This test fails if a currently-gated operation's 403
    // response is ever removed from the committed spec.
    [Theory]
    [InlineData("POST", "/api/v1/brands")]
    [InlineData("PUT", "/api/v1/brands/{id}")]
    [InlineData("DELETE", "/api/v1/brands/{id}")]
    [InlineData("POST", "/api/v1/categories")]
    [InlineData("PUT", "/api/v1/categories/{id}")]
    [InlineData("DELETE", "/api/v1/categories/{id}")]
    [InlineData("POST", "/api/v1/locations")]
    [InlineData("PUT", "/api/v1/locations/{id}")]
    [InlineData("DELETE", "/api/v1/locations/{id}")]
    [InlineData("POST", "/api/v1/networks")]
    [InlineData("PUT", "/api/v1/networks/{id}")]
    [InlineData("DELETE", "/api/v1/networks/{id}")]
    [InlineData("POST", "/api/v1/tags")]
    [InlineData("PUT", "/api/v1/tags/{id}")]
    [InlineData("DELETE", "/api/v1/tags/{id}")]
    [InlineData("POST", "/api/v1/owners")]
    [InlineData("PUT", "/api/v1/owners/{id}")]
    [InlineData("DELETE", "/api/v1/owners/{id}")]
    [InlineData("POST", "/api/v1/devices")]
    [InlineData("PUT", "/api/v1/devices/{id}")]
    [InlineData("DELETE", "/api/v1/devices/{id}")]
    [InlineData("POST", "/api/v1/devices/{id}/tags")]
    [InlineData("DELETE", "/api/v1/devices/{id}/tags/{tagId}")]
    [InlineData("PATCH", "/api/v1/devices/{id}/owner")]
    [InlineData("POST", "/api/v1/devices/bulk/update")]
    [InlineData("POST", "/api/v1/imports/commit")]
    public async Task AdminOrMemberGatedOperation_DeclaresForbiddenResponse(string method, string specPath)
    {
        var committedDocument = OpenApiContractAssertions.LoadDocument(await File.ReadAllTextAsync(ResolveCommittedOpenApiPath()), "committed openapi.yaml");

        committedDocument.Paths.TryGetValue(specPath, out var pathItem).Should().BeTrue($"Spec path '{specPath}' should exist.");
        pathItem!.Operations.TryGetValue(ParseOperationType(method), out var operation).Should().BeTrue($"Operation {method} {specPath} should exist.");
        operation!.Responses.Should().ContainKey(
            "403",
            $"{method} {specPath} is gated by AuthorizationPolicies.AdminOrMember and must document a 403 Forbidden response for a Viewer caller.");
    }

    private static Microsoft.OpenApi.Models.OperationType ParseOperationType(string method)
        => method.ToUpperInvariant() switch
        {
            "GET" => Microsoft.OpenApi.Models.OperationType.Get,
            "POST" => Microsoft.OpenApi.Models.OperationType.Post,
            "PUT" => Microsoft.OpenApi.Models.OperationType.Put,
            "PATCH" => Microsoft.OpenApi.Models.OperationType.Patch,
            "DELETE" => Microsoft.OpenApi.Models.OperationType.Delete,
            _ => throw new InvalidOperationException($"Unsupported HTTP method '{method}'.")
        };

    [Fact]
    public async Task BrandsEndpoint_WhenGetByIdCalled_ResponseMatchesCommittedSchema()
    {
        await ResetDatabaseAsync();
        var brand = new Brand(Guid.NewGuid(), $"Brand-{Guid.NewGuid():N}", "https://example.com", "contract");
        await SeedAsync(entities: [brand]);
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/brands/{brand.Id}");

        await AssertJsonContractAsync(response, "GET", "/api/v1/brands/{id}");
    }

    [Fact]
    public async Task CategoriesEndpoint_WhenGetByIdCalled_ResponseMatchesCommittedSchema()
    {
        await ResetDatabaseAsync();
        var category = new Category(Guid.NewGuid(), $"Category-{Guid.NewGuid():N}");
        await SeedAsync(entities: [category]);
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/categories/{category.Id}");

        await AssertJsonContractAsync(response, "GET", "/api/v1/categories/{id}");
    }

    [Fact]
    public async Task DevicesEndpoint_WhenGetByIdCalled_ResponseMatchesCommittedSchema()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        var device = CreateDevice(references, $"Device-{Guid.NewGuid():N}");
        await SeedAsync(entities: [device]);
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/devices/{device.Id}");

        await AssertJsonContractAsync(response, "GET", "/api/v1/devices/{id}");
    }

    [Fact]
    public async Task OwnersEndpoint_WhenGetByIdCalled_ResponseMatchesCommittedSchema()
    {
        await ResetDatabaseAsync();
        var owner = new Owner(Guid.NewGuid(), $"Owner-{Guid.NewGuid():N}", OwnerRole.Member);
        await SeedAsync(entities: [owner]);
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/owners/{owner.Id}");

        await AssertJsonContractAsync(response, "GET", "/api/v1/owners/{id}");
    }

    [Fact]
    public async Task LocationsEndpoint_WhenGetByIdCalled_ResponseMatchesCommittedSchema()
    {
        await ResetDatabaseAsync();
        var location = new Location(Guid.NewGuid(), $"Location-{Guid.NewGuid():N}", LocationType.Home);
        await SeedAsync(entities: [location]);
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/locations/{location.Id}");

        await AssertJsonContractAsync(response, "GET", "/api/v1/locations/{id}");
    }

    [Fact]
    public async Task NetworksEndpoint_WhenGetByIdCalled_ResponseMatchesCommittedSchema()
    {
        await ResetDatabaseAsync();
        var network = new Network(Guid.NewGuid(), $"Network-{Guid.NewGuid():N}", "contract");
        await SeedAsync(entities: [network]);
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/networks/{network.Id}");

        await AssertJsonContractAsync(response, "GET", "/api/v1/networks/{id}");
    }

    [Fact]
    public async Task TagsEndpoint_WhenGetByIdCalled_ResponseMatchesCommittedSchema()
    {
        await ResetDatabaseAsync();
        var tag = new Tag(Guid.NewGuid(), $"Tag-{Guid.NewGuid():N}", "#445566");
        await SeedAsync(entities: [tag]);
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/tags/{tag.Id}");

        await AssertJsonContractAsync(response, "GET", "/api/v1/tags/{id}");
    }

    [Fact]
    public async Task AuditEventsEndpoint_WhenListed_ResponseMatchesCommittedSchema()
    {
        await ResetDatabaseAsync();
        var auditEvent = new AuditEvent(Guid.NewGuid(), "apone", nameof(Device), Guid.NewGuid().ToString(), AuditAction.Created, "null", "{\"after\":true}");
        await SeedAsync(entities: [auditEvent]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/audit-events?page=1&pageSize=25");

        await AssertJsonContractAsync(response, "GET", "/api/v1/audit-events");
    }

    [Fact]
    public async Task ImportsPreviewEndpoint_WhenCalled_ResponseMatchesCommittedSchema()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();
        using var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(Encoding.UTF8.GetBytes("Title,Brand\nDevice,Brand")), "file", "contract-preview.csv" }
        };

        var response = await client.PostAsync("/api/v1/imports/preview", content);

        await AssertJsonContractAsync(response, "POST", "/api/v1/imports/preview");
    }

    [Fact(Skip = WaitingOnExportResponseSchemaSkip)]
    public async Task ExportsEndpoint_WhenJsonRequested_ResponseMatchesCommittedSchema()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/exports/devices?format=json");

        await AssertJsonContractAsync(response, "GET", "/api/v1/exports/devices");
    }

    private async Task AssertJsonContractAsync(HttpResponseMessage response, string method, string specPath)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.Should().NotBeNull();

        var committedDocument = OpenApiContractAssertions.LoadDocument(await File.ReadAllTextAsync(ResolveCommittedOpenApiPath()), "committed openapi.yaml");
        var body = await response.Content.ReadAsStringAsync();
        var node = string.IsNullOrWhiteSpace(body) ? null : JsonNode.Parse(body);
        OpenApiContractAssertions.AssertResponseMatchesSchema(
            committedDocument,
            method,
            specPath,
            ((int)response.StatusCode).ToString(),
            response.Content.Headers.ContentType!.MediaType ?? "application/json",
            node);
    }

    private static string ResolveCommittedOpenApiPath()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var candidatePaths = new[]
        {
            Path.Combine(repositoryRoot, "openapi.yaml"),
            Path.Combine(repositoryRoot, "src", "TechInventory.Api", "openapi.yaml")
        };

        return candidatePaths.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException("Committed OpenAPI document was not found in the repository root or src\\TechInventory.Api.");
    }
}
