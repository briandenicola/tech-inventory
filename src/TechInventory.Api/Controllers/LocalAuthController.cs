using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechInventory.Api.Common;
using TechInventory.Application.Auth.Commands;
using TechInventory.Application.Common.Results;

namespace TechInventory.Api.Controllers;

/// <summary>
/// F025 — break-glass local credential endpoints. Entirely separate from the
/// Entra-mediated routes; uses its own HS256 JWT issuer (see
/// <c>HmacJwtLocalTokenIssuer</c>).
/// </summary>
[ApiController]
[Route("api/v1/auth/local")]
public sealed class LocalAuthController(ISender sender) : ControllerBase
{
    /// <summary>Exchange username + password for a local JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LocalLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LocalLoginResponse>> Login([FromBody] LocalLoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await sender.Send(new LocalLoginCommand(request.Username, request.Password), cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        // Treat any "InvalidCredentials" failure as 401 so callers can't tell
        // bad-password from no-such-user. Anything else (e.g. Validation from
        // FluentValidation) still bubbles through ApiExceptionHandler.
        if (string.Equals(result.Error?.Code, "InvalidCredentials", StringComparison.Ordinal))
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = result.Error!.Message,
                Type = "https://tools.ietf.org/html/rfc9110#name-401-unauthorized"
            });
        }

        // Force the global handler to render the right ProblemDetails.
        result.EnsureSuccessOrThrow();
        return Unauthorized();
    }

    /// <summary>Rotate the password for the currently authenticated local user.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangeLocalPasswordRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authMethod = User.FindFirstValue("auth_method");
        if (!string.Equals(authMethod, "local", StringComparison.Ordinal))
        {
            return Forbid();
        }

        var subjectId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(subjectId, out var localUserId))
        {
            return Unauthorized();
        }

        var result = await sender.Send(
            new ChangeLocalPasswordCommand(localUserId, request.CurrentPassword, request.NewPassword),
            cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        if (string.Equals(result.Error?.Code, "InvalidCredentials", StringComparison.Ordinal))
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = result.Error!.Message
            });
        }

        result.EnsureSuccessOrThrow();
        return NoContent();
    }
}

public sealed record LocalLoginRequest(string Username, string Password);

public sealed record ChangeLocalPasswordRequest(string CurrentPassword, string NewPassword);
