using FluentValidation;
using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Reports.Queries;

public sealed record GetSummaryReportQuery() : IRequest<Result<SummaryReportResponse>>;

public sealed class GetSummaryReportQueryHandler(IReportingRepository reportingRepository)
    : IRequestHandler<GetSummaryReportQuery, Result<SummaryReportResponse>>
{
    public async Task<Result<SummaryReportResponse>> Handle(GetSummaryReportQuery request, CancellationToken cancellationToken)
    {
        var response = await reportingRepository.GetSummaryAsync(5, cancellationToken).ConfigureAwait(false);
        return Result<SummaryReportResponse>.Success(response);
    }
}

public sealed class GetSummaryReportQueryValidator : AbstractValidator<GetSummaryReportQuery>
{
}
