using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TechInventory.IntegrationTests.Support;

namespace TechInventory.IntegrationTests.Controllers;

public sealed class ReferenceBulkDeleteAuthorizationTests(MemberRoleIntegrationTestFactory<ReferenceBulkDeleteAuthorizationTests> factory)
    : IClassFixture<MemberRoleIntegrationTestFactory<ReferenceBulkDeleteAuthorizationTests>>
{
    private MemberRoleIntegrationTestFactory<ReferenceBulkDeleteAuthorizationTests> Factory { get; } = factory;

    [Theory]
    [InlineData("/api/v1/brands/bulk/delete")]
    [InlineData("/api/v1/categories/bulk/delete")]
    [InlineData("/api/v1/locations/bulk/delete")]
    [InlineData("/api/v1/networks/bulk/delete")]
    public async Task BulkDeleteEndpoint_WhenCallerIsMember_ReturnsForbidden(string route)
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsync(route, JsonContent.Create(new { }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
