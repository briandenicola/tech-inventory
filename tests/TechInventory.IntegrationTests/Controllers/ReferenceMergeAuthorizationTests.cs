using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TechInventory.IntegrationTests.Support;

namespace TechInventory.IntegrationTests.Controllers;

public sealed class ReferenceMergeAuthorizationTests(MemberRoleIntegrationTestFactory<ReferenceMergeAuthorizationTests> factory)
    : IClassFixture<MemberRoleIntegrationTestFactory<ReferenceMergeAuthorizationTests>>
{
    private MemberRoleIntegrationTestFactory<ReferenceMergeAuthorizationTests> Factory { get; } = factory;

    [Theory]
    [InlineData("/api/v1/brands/merge")]
    [InlineData("/api/v1/categories/merge")]
    [InlineData("/api/v1/locations/merge")]
    [InlineData("/api/v1/networks/merge")]
    public async Task MergeEndpoint_WhenCallerIsMember_ReturnsForbidden(string route)
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsync(route, JsonContent.Create(new
        {
            sourceId = Guid.NewGuid(),
            targetId = Guid.NewGuid(),
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
