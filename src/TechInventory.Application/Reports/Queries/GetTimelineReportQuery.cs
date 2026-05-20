using FluentValidation;
using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Common.Validation;

namespace TechInventory.Application.Reports.Queries;

public sealed record GetTimelineReportQuery(
    Guid? CategoryId = null,
    string GroupBy = nameof(TimelineGroupBy.Category),
    DateOnly? FromDate = null,
    DateOnly? ToDate = null) : IRequest<Result<TimelineReportResponse>>;

public sealed class GetTimelineReportQueryHandler(IReportingRepository reportingRepository, TimeProvider timeProvider)
    : IRequestHandler<GetTimelineReportQuery, Result<TimelineReportResponse>>
{
    public async Task<Result<TimelineReportResponse>> Handle(GetTimelineReportQuery request, CancellationToken cancellationToken)
    {
        var groupBy = ParseGroupBy(request.GroupBy);
        var entries = await reportingRepository
            .GetTimelineReportAsync(request.CategoryId, groupBy, request.FromDate, request.ToDate, cancellationToken)
            .ConfigureAwait(false);
        var asOfDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        return Result<TimelineReportResponse>.Success(new TimelineReportResponse(entries, asOfDate, groupBy, request.CategoryId));
    }

    private static TimelineGroupBy ParseGroupBy(string groupBy)
        => Enum.Parse<TimelineGroupBy>(groupBy.Trim(), ignoreCase: true);
}

public sealed class GetTimelineReportQueryValidator : AbstractValidator<GetTimelineReportQuery>
{
    private static readonly string[] AllowedGroupByValues = Enum.GetNames<TimelineGroupBy>();

    public GetTimelineReportQueryValidator()
    {
        RuleFor(query => query.CategoryId)
            .Must(ValidationRules.BeValidOptionalGuid)
            .WithMessage("CategoryId must be a non-empty GUID when provided.");

        RuleFor(query => query.GroupBy)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("GroupBy is required.")
            .Must(groupBy => ValidationRules.BeValidSort(groupBy, AllowedGroupByValues))
            .WithMessage($"GroupBy must be one of: {string.Join(", ", AllowedGroupByValues)}.");

        RuleFor(query => query)
            .Must(query => !query.FromDate.HasValue || !query.ToDate.HasValue || query.FromDate <= query.ToDate)
            .WithMessage("FromDate cannot be greater than ToDate.");
    }
}
