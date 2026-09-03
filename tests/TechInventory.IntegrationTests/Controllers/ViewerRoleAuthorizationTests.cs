using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TechInventory.IntegrationTests.Support;

namespace TechInventory.IntegrationTests.Controllers;

/// <summary>
/// H-04 — real-pipeline coverage for the Viewer role. Mirrors the existing
/// Member-role authorization suites (<see cref="ReferenceMergeAuthorizationTests"/>,
/// <see cref="ReferenceBulkDeleteAuthorizationTests"/>, <see cref="AuditEventsAuthorizationTests"/>)
/// but stamps every request as Viewer via <see cref="ViewerRoleIntegrationTestFactory{T}"/>,
/// so a caller with the least-privileged role is denied the same Admin-gated
/// reference-data mutations as Member — using the real authorization policy
/// pipeline, not a controller-direct call.
///
/// Also covers device create/update/delete/tag/claim-ownership/bulk-update
/// and import commit. These were previously gated only by <c>[Authorize]</c>
/// (any authenticated role), letting Viewer mutate — a genuine authorization
/// gap versus constitution §5.2's Admin/Member/Viewer model. DevicesController
/// and ImportsController now apply
/// <see cref="TechInventory.Api.Authentication.AuthorizationPolicies.AdminOrMember"/>
/// to those actions, so Viewer is rejected the same way it already is for
/// the Admin-only merge/bulk-delete/audit endpoints below.
///
/// Post-review (B1) addition: ordinary reference-entity create/update/delete
/// on Brands, Categories, Locations, Networks, Tags, and Owners carried the
/// identical bare-<c>[Authorize]</c> gap — coupled to, but not covered by,
/// the original H-04 scope (which only asserted the Admin-only
/// <c>merge</c>/<c>bulk/delete</c> reference actions). Those six controllers
/// now apply <see cref="TechInventory.Api.Authentication.AuthorizationPolicies.AdminOrMember"/>
/// to their ordinary CRUD mutations too, exercised below by
/// <c>CreateReferenceEntity_WhenCallerIsViewer_ReturnsForbidden</c>,
/// <c>UpdateReferenceEntity_WhenCallerIsViewer_ReturnsForbidden</c>, and
/// <c>DeleteReferenceEntity_WhenCallerIsViewer_ReturnsForbidden</c>. Existing
/// Admin-role coverage for these same actions (e.g. <c>BrandsControllerTests</c>,
/// <c>CategoriesControllerTests</c>, <c>LocationsControllerTests</c>,
/// <c>NetworksControllerTests</c>) and the Member-positive coverage in
/// <see cref="DeviceAndImportMemberAuthorizationTests"/> continue to prove the
/// policy still allows Admin/Member, not just Admin.
///
/// The real-JWT-pipeline counterpart in
/// <c>AuthIntegrationTests.ViewerRoleOnAdminEndpoint_Returns403Forbidden</c>
/// was permanently <c>[Fact(Skip = ...)]</c>'d (the test factory cannot swap
/// production JWKS discovery for an in-memory signing key) and was removed —
/// it never ran, so it never provided the coverage some documentation had
/// cited it for. This class, exercised through
/// <see cref="ViewerRoleIntegrationTestFactory{T}"/>'s <c>TestAuthHandler</c>,
/// is the real, executing proof that the ASP.NET Core role-authorization
/// pipeline (not a mock) rejects Viewer on every Admin/Member-gated mutation.
/// </summary>
public sealed class ViewerRoleAuthorizationTests(ViewerRoleIntegrationTestFactory<ViewerRoleAuthorizationTests> factory)
    : IClassFixture<ViewerRoleIntegrationTestFactory<ViewerRoleAuthorizationTests>>
{
    private ViewerRoleIntegrationTestFactory<ViewerRoleAuthorizationTests> Factory { get; } = factory;

    [Theory]
    [InlineData("/api/v1/brands/merge")]
    [InlineData("/api/v1/categories/merge")]
    [InlineData("/api/v1/locations/merge")]
    [InlineData("/api/v1/networks/merge")]
    public async Task MergeEndpoint_WhenCallerIsViewer_ReturnsForbidden(string route)
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsync(route, JsonContent.Create(new
        {
            sourceId = Guid.NewGuid(),
            targetId = Guid.NewGuid(),
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("/api/v1/brands/bulk/delete")]
    [InlineData("/api/v1/categories/bulk/delete")]
    [InlineData("/api/v1/locations/bulk/delete")]
    [InlineData("/api/v1/networks/bulk/delete")]
    public async Task BulkDeleteEndpoint_WhenCallerIsViewer_ReturnsForbidden(string route)
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsync(route, JsonContent.Create(new { }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BulkDeleteDevicesEndpoint_WhenCallerIsViewer_ReturnsForbidden()
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsync("/api/v1/devices/bulk/delete", JsonContent.Create(new
        {
            deviceIds = new[] { Guid.NewGuid() },
            reason = "Viewer should not be able to bulk delete devices"
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAuditEvents_WhenCallerIsViewer_ReturnsForbidden()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/v1/audit-events");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateDevice_WhenCallerIsViewer_ReturnsForbidden()
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsync("/api/v1/devices", JsonContent.Create(new
        {
            name = "Viewer Cannot Create",
            categoryId = Guid.NewGuid(),
            ownerId = Guid.NewGuid(),
            locationId = Guid.NewGuid(),
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateDevice_WhenCallerIsViewer_ReturnsForbidden()
    {
        using var client = Factory.CreateClient();

        var response = await client.PutAsync($"/api/v1/devices/{Guid.NewGuid()}", JsonContent.Create(new
        {
            name = "Viewer Cannot Update",
            categoryId = Guid.NewGuid(),
            ownerId = Guid.NewGuid(),
            locationId = Guid.NewGuid(),
            currencyCode = "USD",
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteDevice_WhenCallerIsViewer_ReturnsForbidden()
    {
        using var client = Factory.CreateClient();

        var response = await client.DeleteAsync($"/api/v1/devices/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddDeviceTag_WhenCallerIsViewer_ReturnsForbidden()
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/v1/devices/{Guid.NewGuid()}/tags",
            JsonContent.Create(new { tagId = Guid.NewGuid() }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveDeviceTag_WhenCallerIsViewer_ReturnsForbidden()
    {
        using var client = Factory.CreateClient();

        var response = await client.DeleteAsync($"/api/v1/devices/{Guid.NewGuid()}/tags/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ClaimDeviceOwnership_WhenCallerIsViewer_ReturnsForbidden()
    {
        using var client = Factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/devices/{Guid.NewGuid()}/owner")
        {
            Content = JsonContent.Create(new { ownerId = Guid.NewGuid() })
        };
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReleaseDeviceOwnership_WhenCallerIsViewer_ReturnsForbidden()
    {
        using var client = Factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/devices/{Guid.NewGuid()}/owner")
        {
            Content = JsonContent.Create(new { ownerId = (Guid?)null })
        };
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BulkUpdateDevices_WhenCallerIsViewer_ReturnsForbidden()
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsync("/api/v1/devices/bulk/update", JsonContent.Create(new
        {
            deviceIds = new[] { Guid.NewGuid() },
            changes = new { status = "Retired" },
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CommitImport_WhenCallerIsViewer_ReturnsForbidden()
    {
        using var client = Factory.CreateClient();
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent("Title,Brand\nDevice,Brand"u8.ToArray());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "devices.csv");

        var response = await client.PostAsync("/api/v1/imports/commit", content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// B1 — ordinary reference-entity create/update/delete on Brands,
    /// Categories, Locations, Networks, Tags, and Owners previously carried
    /// only a bare <c>[Authorize]</c> (any authenticated role), the same
    /// coupled gap H-04 found and fixed on Devices/Imports. All six now apply
    /// <see cref="TechInventory.Api.Authentication.AuthorizationPolicies.AdminOrMember"/>.
    /// Authorization runs before model binding, so an intentionally minimal
    /// body is sufficient to prove the 403 — mirrors the existing
    /// <c>MergeEndpoint_WhenCallerIsViewer_ReturnsForbidden</c> /
    /// <c>BulkDeleteEndpoint_WhenCallerIsViewer_ReturnsForbidden</c> pattern
    /// above. Admin positive-path coverage for these same routes already
    /// exists in <c>BrandsControllerTests</c>, <c>CategoriesControllerTests</c>,
    /// <c>LocationsControllerTests</c>, <c>NetworksControllerTests</c>,
    /// <c>TagsControllerTests</c>, and <c>OwnersControllerTests</c> (all run
    /// under the default Admin-role test factory).
    /// </summary>
    [Theory]
    [InlineData("/api/v1/brands")]
    [InlineData("/api/v1/categories")]
    [InlineData("/api/v1/locations")]
    [InlineData("/api/v1/networks")]
    [InlineData("/api/v1/tags")]
    [InlineData("/api/v1/owners")]
    public async Task CreateReferenceEntity_WhenCallerIsViewer_ReturnsForbidden(string route)
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsync(route, JsonContent.Create(new { }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("/api/v1/brands")]
    [InlineData("/api/v1/categories")]
    [InlineData("/api/v1/locations")]
    [InlineData("/api/v1/networks")]
    [InlineData("/api/v1/tags")]
    [InlineData("/api/v1/owners")]
    public async Task UpdateReferenceEntity_WhenCallerIsViewer_ReturnsForbidden(string route)
    {
        using var client = Factory.CreateClient();

        var response = await client.PutAsync($"{route}/{Guid.NewGuid()}", JsonContent.Create(new { }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("/api/v1/brands")]
    [InlineData("/api/v1/categories")]
    [InlineData("/api/v1/locations")]
    [InlineData("/api/v1/networks")]
    [InlineData("/api/v1/tags")]
    [InlineData("/api/v1/owners")]
    public async Task DeleteReferenceEntity_WhenCallerIsViewer_ReturnsForbidden(string route)
    {
        using var client = Factory.CreateClient();

        var response = await client.DeleteAsync($"{route}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// #129/#138 — the new PATCH .../deactivate route carries the same
    /// <see cref="TechInventory.Api.Authentication.AuthorizationPolicies.AdminOrMember"/>
    /// gate as the existing DELETE route it delegates to. Viewer must be
    /// rejected here too, not just on DELETE.
    /// </summary>
    [Theory]
    [InlineData("/api/v1/brands")]
    [InlineData("/api/v1/categories")]
    [InlineData("/api/v1/locations")]
    [InlineData("/api/v1/networks")]
    [InlineData("/api/v1/tags")]
    [InlineData("/api/v1/owners")]
    public async Task DeactivateReferenceEntity_WhenCallerIsViewer_ReturnsForbidden(string route)
    {
        using var client = Factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"{route}/{Guid.NewGuid()}/deactivate");
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // Positive control (Hicks, T102 B4 review): every test above proves Viewer
    // is denied a mutation. Without a passing-read counterpart, a regression
    // that made these endpoints reject Viewer entirely (rather than just
    // gating mutations) would go unnoticed by this class. `GetDevices` and the
    // reference-entity list endpoints carry no `[Authorize(Policy = ...)]`
    // restriction beyond authentication, so Viewer's read access must remain
    // 200 — proving the fix is a mutation gate, not an accidental blanket deny.
    [Theory]
    [InlineData("/api/v1/devices")]
    [InlineData("/api/v1/brands")]
    [InlineData("/api/v1/categories")]
    [InlineData("/api/v1/locations")]
    [InlineData("/api/v1/networks")]
    [InlineData("/api/v1/tags")]
    [InlineData("/api/v1/owners")]
    public async Task GetCollection_WhenCallerIsViewer_ReturnsOk(string route)
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
