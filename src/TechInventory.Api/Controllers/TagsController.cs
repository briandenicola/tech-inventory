using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechInventory.Api.Common;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Tags;
using TechInventory.Application.Tags.Commands;
using TechInventory.Application.Tags.Queries;

namespace TechInventory.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tags")]
public sealed class TagsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<TagResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<TagResponse>>> GetTags(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => this.OkResult(await sender.Send(new ListTagsQuery(page, pageSize, includeInactive), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagResponse>> GetTagById(Guid id, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(new GetTagByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TagResponse>> CreateTag([FromBody] CreateTagRequest request, CancellationToken cancellationToken)
        => this.CreatedAtActionResult(
            nameof(GetTagById),
            await sender.Send(request.ToCommand(), cancellationToken),
            response => new { id = response.Id });

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TagResponse>> UpdateTag(Guid id, [FromBody] UpdateTagRequest request, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToCommand(id), cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteTag(Guid id, CancellationToken cancellationToken)
        => this.NoContentResult(await sender.Send(new DeleteTagCommand(id), cancellationToken));

    public sealed record CreateTagRequest(string Name, string? Color = null)
    {
        public CreateTagCommand ToCommand() => new(Name, Color);
    }

    public sealed record UpdateTagRequest(string Name, string? Color = null)
    {
        public UpdateTagCommand ToCommand(Guid id) => new(id, Name, Color);
    }
}
