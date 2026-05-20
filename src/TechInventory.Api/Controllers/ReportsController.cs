using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechInventory.Api.Common;
using TechInventory.Application.Reports;
using TechInventory.Application.Reports.Queries;

namespace TechInventory.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Member,Viewer")]
[Route("api/v1/reports")]
public sealed class ReportsController(ISender sender) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(SummaryReportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SummaryReportResponse>> GetSummary(CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(new GetSummaryReportQuery(), cancellationToken));

    [HttpGet("warranties")]
    [ProducesResponseType(typeof(WarrantyReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WarrantyReportResponse>> GetWarranties([FromQuery] GetWarrantyReportRequest request, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToQuery(), cancellationToken));

    [HttpGet("spending")]
    [ProducesResponseType(typeof(SpendingReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SpendingReportResponse>> GetSpending([FromQuery] GetSpendingReportRequest request, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToQuery(), cancellationToken));

    public sealed record GetWarrantyReportRequest
    {
        public int ExpiringWithinDays { get; init; } = 30;

        public GetWarrantyReportQuery ToQuery() => new(ExpiringWithinDays);
    }

    public sealed record GetSpendingReportRequest
    {
        public SpendingGroupBy GroupBy { get; init; } = SpendingGroupBy.Month;

        public DateOnly? FromDate { get; init; }

        public DateOnly? ToDate { get; init; }

        public GetSpendingReportQuery ToQuery() => new(GroupBy, FromDate, ToDate);
    }
}
