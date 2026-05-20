using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TechInventory.IntegrationTests.Support;

/// <summary>
/// Integration-test-only authentication handler. Lives in the test project so
/// the production API binary ships zero bypass code. Every request is
/// authenticated as the configured user (defaults to the historical
/// dev-admin identity so the rest of the test suite stays unchanged).
///
/// To stamp requests as a different role (e.g. Member for the audit-events
/// 403 test), tests wire <see cref="TestAuthHandlerOptions.Role"/> via
/// IntegrationTestFactory's role hook (see <c>MemberRoleIntegrationTestFactory</c>).
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<TestAuthHandlerOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<TestAuthHandlerOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestAuth";

    /// <summary>
    /// Stable subject/oid used by integration tests. Reused from the original
    /// dev-admin identity so audit-event assertions and owner auto-provisioning
    /// behavior in existing tests carry over unchanged.
    /// </summary>
    public const string DefaultUserId = "11111111-1111-1111-1111-111111111111";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = string.IsNullOrWhiteSpace(Options.Role) ? "Admin" : Options.Role;
        var userId = string.IsNullOrWhiteSpace(Options.UserId) ? DefaultUserId : Options.UserId;
        var name = string.IsNullOrWhiteSpace(Options.Name) ? "dev-admin" : Options.Name;

        var claims = new[]
        {
            new Claim("sub", userId),
            new Claim("oid", userId),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Role, role),
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public sealed class TestAuthHandlerOptions : AuthenticationSchemeOptions
{
    public string Role { get; set; } = "Admin";
    public string UserId { get; set; } = TestAuthHandler.DefaultUserId;
    public string Name { get; set; } = "dev-admin";
}
