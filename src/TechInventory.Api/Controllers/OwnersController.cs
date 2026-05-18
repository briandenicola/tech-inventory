using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechInventory.Api.Common;
using TechInventory.Application.Abstractions.Services;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Owners;
using TechInventory.Application.Owners.Commands;
using TechInventory.Application.Owners.Queries;
using TechInventory.Domain.Enums;

namespace TechInventory.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/owners")]
public sealed class OwnersController(ISender sender, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<OwnerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<OwnerResponse>>> GetOwners(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => this.OkResult(await sender.Send(new ListOwnersQuery(page, pageSize, includeInactive), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OwnerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OwnerResponse>> GetOwnerById(Guid id, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(new GetOwnerByIdQuery(id), cancellationToken));

    [HttpGet("me")]
    [ProducesResponseType(typeof(OwnerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OwnerResponse>> GetCurrentOwner(CancellationToken cancellationToken)
    {
        var oidString = currentUserService.GetCurrentUserId();
        if (string.IsNullOrEmpty(oidString) || oidString == "system")
        {
            return Unauthorized();
        }

        if (!Guid.TryParse(oidString, out var entraObjectId))
        {
            return Unauthorized();
        }

        return this.OkResult(await sender.Send(new GetOwnerByEntraObjectIdQuery(entraObjectId), cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(OwnerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OwnerResponse>> CreateOwner([FromBody] CreateOwnerRequest request, CancellationToken cancellationToken)
        => this.CreatedAtActionResult(
            nameof(GetOwnerById),
            await sender.Send(request.ToCommand(), cancellationToken),
            response => new { id = response.Id });

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(OwnerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OwnerResponse>> UpdateOwner(Guid id, [FromBody] UpdateOwnerRequest request, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToCommand(id), cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteOwner(Guid id, CancellationToken cancellationToken)
        => this.NoContentResult(await sender.Send(new DeleteOwnerCommand(id), cancellationToken));

    public sealed record CreateOwnerRequest(string DisplayName, OwnerRole Role = OwnerRole.Member, Guid? EntraObjectId = null)
    {
        public CreateOwnerCommand ToCommand() => new(DisplayName, Role, EntraObjectId);
    }

    public sealed record UpdateOwnerRequest(string DisplayName, OwnerRole Role, Guid? EntraObjectId = null)
    {
        public UpdateOwnerCommand ToCommand(Guid id) => new(id, DisplayName, Role, EntraObjectId);
    }
}
