using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechInventory.Api.Authentication;
using TechInventory.Api.Common;
using TechInventory.Application.BulkOperations;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Devices;
using TechInventory.Application.Devices.Commands;
using TechInventory.Application.Devices.Queries;
using TechInventory.Application.Tags;
using TechInventory.Domain.Enums;

namespace TechInventory.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/devices")]
public sealed class DevicesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<DeviceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<DeviceResponse>>> GetDevices([FromQuery] ListDevicesRequest request, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceResponse>> GetDeviceById(Guid id, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(new GetDeviceByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeviceResponse>> CreateDevice([FromBody] CreateDeviceRequest request, CancellationToken cancellationToken)
        => this.CreatedAtActionResult(
            nameof(GetDeviceById),
            await sender.Send(request.ToCommand(), cancellationToken),
            response => new { id = response.Id });

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeviceResponse>> UpdateDevice(Guid id, [FromBody] UpdateDeviceRequest request, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToCommand(id), cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteDevice(Guid id, [FromBody] DeleteDeviceRequest? request, CancellationToken cancellationToken)
        => this.NoContentResult(await sender.Send((request ?? new DeleteDeviceRequest()).ToCommand(id), cancellationToken));

    [HttpPost("{id:guid}/tags")]
    [ProducesResponseType(typeof(DeviceTagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeviceTagResponse>> AddTag(Guid id, [FromBody] AddDeviceTagRequest request, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToCommand(id), cancellationToken));

    [HttpGet("{id:guid}/tags")]
    [ProducesResponseType(typeof(IReadOnlyList<TagResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TagResponse>>> ListTags(Guid id, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(new ListDeviceTagsQuery(id), cancellationToken));

    [HttpDelete("{id:guid}/tags/{tagId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveTag(Guid id, Guid tagId, CancellationToken cancellationToken)
        => this.NoContentResult(await sender.Send(new RemoveTagFromDeviceCommand(id, tagId), cancellationToken));

    [HttpPatch("{id:guid}/owner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ClaimOwnership(Guid id, [FromBody] ClaimDeviceOwnershipRequest request, CancellationToken cancellationToken)
        => this.NoContentResult(await sender.Send(request.ToCommand(id), cancellationToken));

    [HttpPost("bulk/update")]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BulkOperationResponse>> BulkUpdateDevices(
        [FromBody] BulkUpdateDevicesRequest request,
        CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToCommand(), cancellationToken));

    [HttpPost("bulk/delete")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BulkOperationResponse>> BulkDeleteDevices(
        [FromBody] BulkDeleteDevicesRequest request,
        CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToCommand(), cancellationToken));

    public sealed record BulkUpdateDevicesRequest(
        IReadOnlyList<Guid> DeviceIds,
        BulkUpdateDeviceChangesRequest Changes)
    {
        public BulkUpdateDevicesCommand ToCommand()
            => new(DeviceIds ?? Array.Empty<Guid>(), Changes?.ToChanges() ?? new BulkUpdateDeviceChanges());
    }

    public sealed record BulkUpdateDeviceChangesRequest(
        Guid? CategoryId = null,
        Guid? OwnerId = null,
        Guid? BrandId = null,
        Guid? LocationId = null,
        DeviceStatus? Status = null)
    {
        public BulkUpdateDeviceChanges ToChanges()
            => new(CategoryId, OwnerId, BrandId, LocationId, Status);
    }

    public sealed record BulkDeleteDevicesRequest(IReadOnlyList<Guid> DeviceIds, string Reason)
    {
        public BulkDeleteDevicesCommand ToCommand()
            => new(DeviceIds ?? Array.Empty<Guid>(), Reason ?? string.Empty);
    }

    public sealed record ListDevicesRequest
    {
        public int Page { get; init; } = 1;

        public int PageSize { get; init; } = 25;

        public string? Search { get; init; }

        public Guid? BrandId { get; init; }

        public Guid? CategoryId { get; init; }

        public Guid? OwnerId { get; init; }

        public Guid? LocationId { get; init; }

        public Guid? NetworkId { get; init; }

        public DeviceStatus? Status { get; init; }

        public string? Tags { get; init; }

        public int? PurchaseYearFrom { get; init; }

        public int? PurchaseYearTo { get; init; }

        public string? SortBy { get; init; }

        public bool SortDescending { get; init; }

        public ListDevicesQuery ToQuery()
            => new(
                Page,
                PageSize,
                Search,
                BrandId,
                CategoryId,
                OwnerId,
                LocationId,
                NetworkId,
                Status,
                ParseTags(Tags),
                PurchaseYearFrom,
                PurchaseYearTo,
                SortBy,
                SortDescending);

        private static IReadOnlyCollection<Guid>? ParseTags(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(tagId => Guid.TryParse(tagId, out var parsedTagId) ? parsedTagId : Guid.Empty)
                .ToArray();
        }
    }

    public sealed record CreateDeviceRequest(
        string Name,
        Guid? BrandId,
        Guid CategoryId,
        Guid OwnerId,
        Guid LocationId,
        string? CurrencyCode = null,
        string? Model = null,
        string? SerialNumber = null,
        Guid? NetworkId = null,
        DateOnly? PurchaseDate = null,
        decimal? PurchasePrice = null,
        DeviceStatus Status = DeviceStatus.Active,
        string? Notes = null,
        DateOnly? RetiredDate = null,
        string? DisposalMethod = null,
        string? Purpose = null,
        string? OperatingSystem = null,
        string? IpAddress = null,
        string? MacAddress = null,
        string? ProductUrl = null,
        string? Version = null,
        DateOnly? WarrantyExpiry = null)
    {
        public CreateDeviceCommand ToCommand()
            => new(
                Name,
                BrandId,
                CategoryId,
                OwnerId,
                LocationId,
                CurrencyCode,
                Model,
                SerialNumber,
                NetworkId,
                PurchaseDate,
                PurchasePrice,
                Status,
                Notes,
                RetiredDate,
                DisposalMethod,
                Purpose,
                OperatingSystem,
                IpAddress,
                MacAddress,
                ProductUrl,
                Version,
                WarrantyExpiry);
    }

    public sealed record UpdateDeviceRequest(
        string Name,
        Guid? BrandId,
        Guid CategoryId,
        Guid OwnerId,
        Guid LocationId,
        string CurrencyCode,
        string? Model = null,
        string? SerialNumber = null,
        Guid? NetworkId = null,
        DateOnly? PurchaseDate = null,
        decimal? PurchasePrice = null,
        DeviceStatus Status = DeviceStatus.Active,
        string? Notes = null,
        DateOnly? RetiredDate = null,
        string? DisposalMethod = null,
        string? Purpose = null,
        string? OperatingSystem = null,
        string? IpAddress = null,
        string? MacAddress = null,
        string? ProductUrl = null,
        string? Version = null,
        DateOnly? WarrantyExpiry = null)
    {
        public UpdateDeviceCommand ToCommand(Guid id)
            => new(
                id,
                Name,
                BrandId,
                CategoryId,
                OwnerId,
                LocationId,
                CurrencyCode,
                Model,
                SerialNumber,
                NetworkId,
                PurchaseDate,
                PurchasePrice,
                Status,
                Notes,
                RetiredDate,
                DisposalMethod,
                Purpose,
                OperatingSystem,
                IpAddress,
                MacAddress,
                ProductUrl,
                Version,
                WarrantyExpiry);
    }

    public sealed record DeleteDeviceRequest(string? DisposalMethod = null, DateOnly? RetiredDate = null)
    {
        public DeleteDeviceCommand ToCommand(Guid id) => new(id, DisposalMethod, RetiredDate);
    }

    public sealed record AddDeviceTagRequest(Guid TagId)
    {
        public AddTagToDeviceCommand ToCommand(Guid deviceId) => new(deviceId, TagId);
    }

    public sealed record ClaimDeviceOwnershipRequest(Guid OwnerId)
    {
        public ClaimDeviceOwnershipCommand ToCommand(Guid deviceId) => new(deviceId, OwnerId);
    }
}
