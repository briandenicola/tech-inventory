using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechInventory.Api.Common;
using TechInventory.Application.Settings;

namespace TechInventory.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Member,Viewer")]
[Route("api/v1/settings")]
public sealed class SettingsController(ISender sender) : ControllerBase
{
    [HttpGet("display")]
    [ProducesResponseType(typeof(DisplaySettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DisplaySettingsResponse>> GetDisplaySettings(CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(new GetDisplaySettingsQuery(), cancellationToken));

    [HttpPut("display")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DisplaySettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DisplaySettingsResponse>> UpdateDisplaySettings(
        [FromBody] UpdateDisplaySettingsRequest request,
        CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToCommand(), cancellationToken));

    public sealed record UpdateDisplaySettingsRequest
    {
        public string[] DeviceListColumns { get; init; } = [];

        public string[] DeviceDetailFields { get; init; } = [];

        public UpdateDisplaySettingsCommand ToCommand() => new(DeviceListColumns, DeviceDetailFields);
    }
}
