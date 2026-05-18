using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechInventory.Api.Common;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Locations;
using TechInventory.Application.Locations.Commands;
using TechInventory.Application.Locations.Queries;
using TechInventory.Domain.Enums;

namespace TechInventory.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/locations")]
public sealed class LocationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<LocationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<LocationResponse>>> GetLocations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => this.OkResult(await sender.Send(new ListLocationsQuery(page, pageSize, includeInactive), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationResponse>> GetLocationById(Guid id, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(new GetLocationByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LocationResponse>> CreateLocation([FromBody] CreateLocationRequest request, CancellationToken cancellationToken)
        => this.CreatedAtActionResult(
            nameof(GetLocationById),
            await sender.Send(request.ToCommand(), cancellationToken),
            response => new { id = response.Id });

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LocationResponse>> UpdateLocation(Guid id, [FromBody] UpdateLocationRequest request, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToCommand(id), cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteLocation(Guid id, CancellationToken cancellationToken)
        => this.NoContentResult(await sender.Send(new DeleteLocationCommand(id), cancellationToken));

    public sealed record CreateLocationRequest(string Name, LocationType Type)
    {
        public CreateLocationCommand ToCommand() => new(Name, Type);
    }

    public sealed record UpdateLocationRequest(string Name, LocationType Type)
    {
        public UpdateLocationCommand ToCommand(Guid id) => new(id, Name, Type);
    }
}
