using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TechInventory.Api.Authentication;

/// <summary>
/// Terminal rejection target for requests carrying both an API key and a bearer
/// token (ADR 0003, N-11).
/// </summary>
/// <remarks>
/// Implemented as an authentication scheme rather than middleware so the policy
/// scheme can forward to it directly and no concrete handler ever runs. It always
/// fails, so there is no path by which an ambiguous request falls through to Entra,
/// Local, or ApiKey.
/// </remarks>
public sealed class AmbiguousCredentialHandler(
    IOptionsMonitor<AmbiguousCredentialOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder)
    : AuthenticationHandler<AmbiguousCredentialOptions>(options, loggerFactory, encoder)
{
    private const string Message = "Provide either a bearer token or an API key, not both.";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        Logger.LogWarning("Authentication rejected: request presented both ApiKey and Bearer credentials.");
        return Task.FromResult(AuthenticateResult.Fail(Message));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/problem+json";
        await Response.WriteAsJsonAsync(
            ApiKeyProblemDetailsFactory.Create(
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                Message,
                ApiKeyProblemDetailsFactory.AmbiguousCredentialCode,
                Context.TraceIdentifier),
            Context.RequestAborted).ConfigureAwait(false);
    }
}

public sealed class AmbiguousCredentialOptions : AuthenticationSchemeOptions;
