using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechInventory.Api.Authentication;
using TechInventory.Api.Common;
using TechInventory.Api.Features.Categories;
using TechInventory.Application.BulkOperations;
using TechInventory.Application.Categories;
using TechInventory.Application.Categories.Commands;
using TechInventory.Application.Categories.Queries;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Merges;

namespace TechInventory.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/categories")]
public sealed class CategoriesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<CategoryResponse>>> GetCategories(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => this.OkResult(await sender.Send(new ListCategoriesQuery(page, pageSize, includeInactive), cancellationToken));

    [HttpGet("tree")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetCategoryTree([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetCategoryTreeQuery(includeInactive), cancellationToken);
        return Ok(result.GetValueOrThrow());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> GetCategoryById(Guid id, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(new GetCategoryByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> CreateCategory([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
        => this.CreatedAtActionResult(
            nameof(GetCategoryById),
            await sender.Send(request.ToCommand(), cancellationToken),
            response => new { id = response.Id });

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToCommand(id), cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
        => this.NoContentResult(await sender.Send(new DeleteCategoryCommand(id), cancellationToken));

    [HttpPost("merge")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(MergeReferenceEntityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MergeReferenceEntityResponse>> MergeCategories([FromBody] MergeCategoryRequest request, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToCommand(), cancellationToken));

    [HttpPost("bulk/delete")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BulkOperationResponse>> BulkDeleteCategories([FromBody] BulkDeleteCategoriesRequest request, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToCommand(), cancellationToken));

    public sealed record CreateCategoryRequest(string Name, Guid? ParentId = null, string? Icon = null)
    {
        public CreateCategoryCommand ToCommand() => new(Name, ParentId, Icon);
    }

    public sealed record UpdateCategoryRequest(string Name, Guid? ParentId = null, string? Icon = null)
    {
        public UpdateCategoryCommand ToCommand(Guid id) => new(id, Name, ParentId, Icon);
    }

    public sealed record MergeCategoryRequest(Guid SourceId, Guid TargetId)
    {
        public MergeCategoryCommand ToCommand() => new(SourceId, TargetId);
    }

    public sealed record BulkDeleteCategoriesRequest(IReadOnlyList<Guid> CategoryIds)
    {
        public BulkDeleteCategoriesCommand ToCommand() => new(CategoryIds ?? Array.Empty<Guid>());
    }
}
