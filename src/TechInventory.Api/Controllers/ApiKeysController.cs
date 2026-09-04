using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TechInventory.Api.Authentication;
using TechInventory.Api.Common;
using TechInventory.Application.ApiKeys;
using TechInventory.Application.ApiKeys.Commands;
using TechInventory.Application.ApiKeys.Queries;
using TechInventory.Application.Common.Paging;
using TechInventory.Domain.Enums;

namespace TechInventory.Api.Controllers;

/// <summary>
/// Personal API key management (#149).
/// </summary>
/// <remarks>
/// Bearer-only. <see cref="RejectApiKeyAuthenticationAttribute"/> blocks
/// API-key-authenticated callers from every action here, so a key can never mint or
/// revoke keys — including its own (N-10). That guard is defence in depth: the
/// scope allow-list in <see cref="ApiKeyScopeAuthorizationHandler"/> already excludes
/// this route, and both must be removed before a key could reach it.
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOrMember)]
[RejectApiKeyAuthentication]
[Route("api/v1/api-keys")]
public sealed class ApiKeysController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ApiKeyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<ApiKeyResponse>>> GetApiKeys(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
        => this.OkResult(await sender.Send(new ListApiKeysQuery(page, pageSize), cancellationToken));

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResponse<ApiKeyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<ApiKeyResponse>>> GetAllApiKeys(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
        => this.OkResult(await sender.Send(new ListAllApiKeysQuery(page, pageSize), cancellationToken));

    /// <summary>
    /// Creates a key. The response carries the only copy of the plaintext credential
    /// that will ever exist — it is not stored and cannot be retrieved again.
    /// </summary>
    [HttpPost]
    [EnableRateLimiting(ApiKeyRateLimiting.PolicyName)]
    [ProducesResponseType(typeof(CreatedApiKeyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatedApiKeyResponse>> CreateApiKey(
        [FromBody] CreateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await sender.Send(request.ToCommand(), cancellationToken);
        var value = result.GetValueOrThrow();

        // No GET-by-id route exists by design: there is nothing to fetch afterwards
        // that the list does not already return, so this reports the collection.
        return Created($"/api/v1/api-keys/{value.Id}", value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeApiKey(Guid id, CancellationToken cancellationToken)
        => this.NoContentResult(await sender.Send(new RevokeApiKeyCommand(id), cancellationToken));
}

/// <summary>Request body for key creation. <c>scope</c> is the wire name, e.g. <c>inventory.read</c>.</summary>
public sealed record CreateApiKeyRequest(string Name, string Scope, int? ExpiresInDays = null)
{
    public CreateApiKeyCommand ToCommand()
        => new(Name, ParseScope(Scope), ExpiresInDays);

    /// <summary>
    /// Maps the wire value to the enum. An unrecognised value maps to a sentinel that
    /// fails the <c>IsInEnum</c> validator, so a typo yields a 400 naming the field
    /// rather than silently defaulting to a scope the caller did not ask for.
    /// </summary>
    private static ApiKeyScope ParseScope(string? scope) => scope switch
    {
        ApiKeyScopeNames.Read => ApiKeyScope.Read,
        ApiKeyScopeNames.Write => ApiKeyScope.Write,
        _ => (ApiKeyScope)0,
    };
}
