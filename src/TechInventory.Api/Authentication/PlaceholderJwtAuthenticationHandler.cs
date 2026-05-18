using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TechInventory.Api.Authentication;

public sealed class PlaceholderJwtAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string NotConfiguredMessage = "JWT bearer authentication is not configured. Development requests require Auth:DevBypass" + "=true.";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.Fail(NotConfiguredMessage));
}
