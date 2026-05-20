using FluentValidation;
using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Common.Validation;

namespace TechInventory.Application.Reports.Queries;

public sealed record GetEraReportQuery(Guid? CategoryId = null) : IRequest<Result<EraReportResponse>>;

public sealed class GetEraReportQueryHandler(IReportingRepository reportingRepository, TimeProvider timeProvider)
    : IRequestHandler<GetEraReportQuery, Result<EraReportResponse>>
{
    public async Task<Result<EraReportResponse>> Handle(GetEraReportQuery request, CancellationToken cancellationToken)
    {
        var decades = await reportingRepository.GetEraReportAsync(request.CategoryId, cancellationToken).ConfigureAwait(false);
        var asOfDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        return Result<EraReportResponse>.Success(new EraReportResponse(decades, asOfDate, request.CategoryId));
    }
}

public sealed class GetEraReportQueryValidator : AbstractValidator<GetEraReportQuery>
{
    public GetEraReportQueryValidator()
    {
        RuleFor(query => query.CategoryId)
            .Must(ValidationRules.BeValidOptionalGuid)
            .WithMessage("CategoryId must be a non-empty GUID when provided.");
    }
}
