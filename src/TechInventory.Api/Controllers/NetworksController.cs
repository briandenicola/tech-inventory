using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechInventory.Api.Authentication;
using TechInventory.Api.Common;
using TechInventory.Application.BulkOperations;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Merges;
using TechInventory.Application.Networks;
using TechInventory.Application.Networks.Commands;
using TechInventory.Application.Networks.Queries;

namespace TechInventory.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/networks")]
public sealed class NetworksController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<NetworkResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<NetworkResponse>>> GetNetworks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => this.OkResult(await sender.Send(new ListNetworksQuery(page, pageSize, includeInactive), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NetworkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NetworkResponse>> GetNetworkById(Guid id, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(new GetNetworkByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(NetworkResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<NetworkResponse>> CreateNetwork([FromBody] CreateNetworkRequest request, CancellationToken cancellationToken)
        => this.CreatedAtActionResult(
            nameof(GetNetworkById),
            await sender.Send(request.ToCommand(), cancellationToken),
            response => new { id = response.Id });

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(NetworkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<NetworkResponse>> UpdateNetwork(Guid id, [FromBody] UpdateNetworkRequest request, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToCommand(id), cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteNetwork(Guid id, CancellationToken cancellationToken)
        => this.NoContentResult(await sender.Send(new DeleteNetworkCommand(id), cancellationToken));

    [HttpPost("merge")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(MergeReferenceEntityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MergeReferenceEntityResponse>> MergeNetworks([FromBody] MergeNetworkRequest request, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToCommand(), cancellationToken));

    [HttpPost("bulk/delete")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BulkOperationResponse>> BulkDeleteNetworks([FromBody] BulkDeleteNetworksRequest request, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToCommand(), cancellationToken));

    public sealed record CreateNetworkRequest(string Name, string? Description = null)
    {
        public CreateNetworkCommand ToCommand() => new(Name, Description);
    }

    public sealed record UpdateNetworkRequest(string Name, string? Description = null)
    {
        public UpdateNetworkCommand ToCommand(Guid id) => new(id, Name, Description);
    }

    public sealed record MergeNetworkRequest(Guid SourceId, Guid TargetId)
    {
        public MergeNetworkCommand ToCommand() => new(SourceId, TargetId);
    }

    public sealed record BulkDeleteNetworksRequest(IReadOnlyList<Guid> NetworkIds)
    {
        public BulkDeleteNetworksCommand ToCommand() => new(NetworkIds ?? Array.Empty<Guid>());
    }
}
