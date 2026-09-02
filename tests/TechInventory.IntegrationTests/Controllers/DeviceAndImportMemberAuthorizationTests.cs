using System.Net;
using System.Text;
using FluentAssertions;
using TechInventory.IntegrationTests.Support;

namespace TechInventory.IntegrationTests.Controllers;

/// <summary>
/// Positive-path counterpart to <see cref="ViewerRoleAuthorizationTests"/>'s
/// device/import coverage. Proves that gating <c>POST /api/v1/devices</c> and
/// <c>POST /api/v1/imports/commit</c> behind
/// <see cref="TechInventory.Api.Authentication.AuthorizationPolicies.AdminOrMember"/>
/// still lets a Member — not just Admin — perform these mutations, per
/// constitution §5.2 ("Admin/Member may mutate where permitted").
/// </summary>
public sealed class DeviceAndImportMemberAuthorizationTests(MemberRoleIntegrationTestFactory<DeviceAndImportMemberAuthorizationTests> factory)
    : ControllerTestBase<DeviceAndImportMemberAuthorizationTests>(factory), IClassFixture<MemberRoleIntegrationTestFactory<DeviceAndImportMemberAuthorizationTests>>
{
    [Fact]
    public async Task CreateDevice_WhenCallerIsMember_ReturnsCreated()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        using var client = CreateClient();

        var response = await client.PostAsync("/api/v1/devices", CreateJsonContent(new
        {
            name = $"Member-Created-{Guid.NewGuid():N}",
            brandId = references.Brand.Id,
            categoryId = references.Category.Id,
            ownerId = references.Owner.Id,
            locationId = references.Location.Id,
            currencyCode = "USD",
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CommitImport_WhenCallerIsMember_ReturnsCreated()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        using var client = CreateClient();

        var csv = string.Join(
            Environment.NewLine,
            "Title,Brand,Category,Owner,Location,Purchase Date,Purchase Price,Status,Notes",
            $"Member Import Device,{references.Brand.Name},{references.Category.Name},{references.Owner.DisplayName},{references.Location.Name},2024-05-01,199.99,Active,Imported by Member");

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "member-import.csv");

        var response = await client.PostAsync("/api/v1/imports/commit", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
