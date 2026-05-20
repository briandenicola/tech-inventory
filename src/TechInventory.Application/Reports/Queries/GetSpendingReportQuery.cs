using FluentValidation;
using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Reports.Queries;

public sealed record GetSpendingReportQuery(
    SpendingGroupBy GroupBy = SpendingGroupBy.Month,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null) : IRequest<Result<SpendingReportResponse>>;

public sealed class GetSpendingReportQueryHandler(IReportingRepository reportingRepository)
    : IRequestHandler<GetSpendingReportQuery, Result<SpendingReportResponse>>
{
    public async Task<Result<SpendingReportResponse>> Handle(GetSpendingReportQuery request, CancellationToken cancellationToken)
    {
        var periods = await reportingRepository
            .GetSpendingAsync(request.GroupBy, request.FromDate, request.ToDate, cancellationToken)
            .ConfigureAwait(false);

        return Result<SpendingReportResponse>.Success(new SpendingReportResponse(request.GroupBy, request.FromDate, request.ToDate, periods));
    }
}

public sealed class GetSpendingReportQueryValidator : AbstractValidator<GetSpendingReportQuery>
{
    public GetSpendingReportQueryValidator()
    {
        RuleFor(query => query)
            .Must(query => !query.FromDate.HasValue || !query.ToDate.HasValue || query.FromDate <= query.ToDate)
            .WithMessage("FromDate cannot be greater than ToDate.");
    }
}
