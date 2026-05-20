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

    [HttpGet("eras")]
    [ProducesResponseType(typeof(EraReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EraReportResponse>> GetEras([FromQuery] GetEraReportRequest request, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToQuery(), cancellationToken));

    [HttpGet("timeline")]
    [ProducesResponseType(typeof(TimelineReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TimelineReportResponse>> GetTimeline([FromQuery] GetTimelineReportRequest request, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(request.ToQuery(), cancellationToken));

    [HttpGet("insurance")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetInsurance([FromQuery] GetInsuranceReportRequest request, CancellationToken cancellationToken)
    {
        var report = (await sender.Send(request.ToQuery(), cancellationToken).ConfigureAwait(false)).GetValueOrThrow();
        return File(report.Content, "text/csv", report.FileName);
    }

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

    public sealed record GetEraReportRequest
    {
        [FromQuery(Name = "categoryId")]
        public Guid? CategoryId { get; init; }

        public GetEraReportQuery ToQuery() => new(CategoryId);
    }

    public sealed record GetTimelineReportRequest
    {
        [FromQuery(Name = "categoryId")]
        public Guid? CategoryId { get; init; }

        [FromQuery(Name = "groupBy")]
        public string GroupBy { get; init; } = nameof(TimelineGroupBy.Category);

        [FromQuery(Name = "fromDate")]
        public DateOnly? FromDate { get; init; }

        [FromQuery(Name = "toDate")]
        public DateOnly? ToDate { get; init; }

        public GetTimelineReportQuery ToQuery() => new(CategoryId, GroupBy, FromDate, ToDate);
    }

    public sealed record GetInsuranceReportRequest
    {
        public Guid? LocationId { get; init; }

        public GetInsuranceReportQuery ToQuery() => new(LocationId);
    }
}
