using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechInventory.Api.Authentication;
using TechInventory.Api.Common;
using TechInventory.Api.Features.AuditEvents;
using TechInventory.Application.Common.Paging;
using TechInventory.Domain.Enums;

namespace TechInventory.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Admin)]
[Route("api/v1/audit-events")]
public sealed class AuditEventsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AuditEventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<AuditEventResponse>>> GetAuditEvents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] AuditAction? action = null,
        [FromQuery] string? actor = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
        => this.OkResult(await sender.Send(
            new ListAuditEventsQuery(
                page,
                pageSize,
                entityType,
                entityId,
                action,
                actor,
                from,
                to),
            cancellationToken));
}
