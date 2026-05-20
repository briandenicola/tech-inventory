using FluentValidation;
using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Reports.Queries;

public sealed record GetWarrantyReportQuery(int ExpiringWithinDays = 30) : IRequest<Result<WarrantyReportResponse>>;

public sealed class GetWarrantyReportQueryHandler(IReportingRepository reportingRepository)
    : IRequestHandler<GetWarrantyReportQuery, Result<WarrantyReportResponse>>
{
    public async Task<Result<WarrantyReportResponse>> Handle(GetWarrantyReportQuery request, CancellationToken cancellationToken)
    {
        var asOfDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var devices = await reportingRepository
            .GetExpiringWarrantiesAsync(asOfDate, request.ExpiringWithinDays, cancellationToken)
            .ConfigureAwait(false);

        return Result<WarrantyReportResponse>.Success(new WarrantyReportResponse(asOfDate, request.ExpiringWithinDays, devices));
    }
}

public sealed class GetWarrantyReportQueryValidator : AbstractValidator<GetWarrantyReportQuery>
{
    public GetWarrantyReportQueryValidator()
    {
        RuleFor(query => query.ExpiringWithinDays)
            .InclusiveBetween(1, 365);
    }
}
