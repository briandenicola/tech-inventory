using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace TechInventory.Api.Authentication;

public sealed class DevBypassAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string DevUserId = "11111111-1111-1111-1111-111111111111";
    private const string DefaultRole = "Admin";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Allow integration tests + ad-hoc local validation to assume a
        // non-Admin role via Auth:DevBypassRole. Defaults to Admin so the
        // historical local-dev experience is unchanged.
        var role = configuration["Auth:DevBypassRole"];
        if (string.IsNullOrWhiteSpace(role))
        {
            role = DefaultRole;
        }

        var claims = new[]
        {
            new Claim("sub", DevUserId),
            new Claim("oid", DevUserId),
            new Claim(ClaimTypes.NameIdentifier, DevUserId),
            new Claim(ClaimTypes.Name, "dev-admin"),
            new Claim(ClaimTypes.Role, role),
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
