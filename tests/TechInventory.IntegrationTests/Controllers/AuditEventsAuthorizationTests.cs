using System.Net;
using FluentAssertions;
using TechInventory.IntegrationTests.Support;

namespace TechInventory.IntegrationTests.Controllers;

/// <summary>
/// Negative-path coverage for F021: confirms <c>/api/v1/audit-events</c>
/// rejects non-Admin callers with 403, even when authenticated via the
/// dev-bypass handler stamped as a Member.
/// </summary>
public sealed class AuditEventsAuthorizationTests(MemberRoleIntegrationTestFactory<AuditEventsAuthorizationTests> factory)
    : IClassFixture<MemberRoleIntegrationTestFactory<AuditEventsAuthorizationTests>>
{
    private MemberRoleIntegrationTestFactory<AuditEventsAuthorizationTests> Factory { get; } = factory;

    [Fact]
    public async Task GetAuditEvents_WhenCallerIsMember_ReturnsForbidden()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/v1/audit-events");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
