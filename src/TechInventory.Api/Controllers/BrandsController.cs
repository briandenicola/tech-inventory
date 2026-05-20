using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechInventory.Api.Authentication;
using TechInventory.Api.Common;
using TechInventory.Application.Brands;
using TechInventory.Application.Brands.Commands;
using TechInventory.Application.Brands.Queries;
using TechInventory.Application.BulkOperations;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Merges;

namespace TechInventory.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/brands")]
public sealed class BrandsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<BrandResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<BrandResponse>>> GetBrands(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => this.OkResult(await sender.Send(new ListBrandsQuery(page, pageSize, includeInactive), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BrandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrandResponse>> GetBrandById(Guid id, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(new GetBrandByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(BrandResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BrandResponse>> CreateBrand([FromBody] CreateBrandRequest request, CancellationToken cancellationToken)
        => this.CreatedAtActionResult(
            nameof(GetBrandById),
            await sender.Send(request.ToCommand(), cancellationToken),
            response => new { id = response.Id });

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BrandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BrandResponse>> UpdateBrand(Guid id, [FromBody] UpdateBrandRequest request, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToCommand(id), cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteBrand(Guid id, CancellationToken cancellationToken)
        => this.NoContentResult(await sender.Send(new DeleteBrandCommand(id), cancellationToken));

    [HttpPost("merge")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(MergeReferenceEntityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MergeReferenceEntityResponse>> MergeBrands([FromBody] MergeBrandRequest request, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToCommand(), cancellationToken));

    [HttpPost("bulk/delete")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BulkOperationResponse>> BulkDeleteBrands([FromBody] BulkDeleteBrandsRequest request, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToCommand(), cancellationToken));

    public sealed record CreateBrandRequest(string Name, string? Website = null, string? Notes = null)
    {
        public CreateBrandCommand ToCommand() => new(Name, Website, Notes);
    }

    public sealed record UpdateBrandRequest(string Name, string? Website = null, string? Notes = null)
    {
        public UpdateBrandCommand ToCommand(Guid id) => new(id, Name, Website, Notes);
    }

    public sealed record MergeBrandRequest(Guid SourceId, Guid TargetId)
    {
        public MergeBrandCommand ToCommand() => new(SourceId, TargetId);
    }

    public sealed record BulkDeleteBrandsRequest(IReadOnlyList<Guid> BrandIds)
    {
        public BulkDeleteBrandsCommand ToCommand() => new(BrandIds ?? Array.Empty<Guid>());
    }
}
