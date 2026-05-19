using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace TechInventory.IntegrationTests.Support;

/// <summary>
/// A test factory variant that overrides <c>Auth:DevBypassRole</c> so the
/// dev-bypass authentication handler stamps requests as a non-Admin role.
/// Used by integration tests that need to assert role-based authorization
/// (e.g. /api/v1/audit-events requires Admin and must 403 for Member).
/// </summary>
public sealed class MemberRoleIntegrationTestFactory<TMarker> : IntegrationTestFactory<TMarker>
    where TMarker : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:DevBypassRole"] = "Member"
            });
        });
    }
}
