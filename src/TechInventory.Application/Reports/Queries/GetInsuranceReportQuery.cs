using System.Globalization;
using System.Text;
using FluentValidation;
using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Common.Validation;

namespace TechInventory.Application.Reports.Queries;

public sealed record GetInsuranceReportQuery(Guid? LocationId = null) : IRequest<Result<InsuranceReportFileResponse>>;

public sealed class GetInsuranceReportQueryHandler(IReportingRepository reportingRepository, TimeProvider timeProvider)
    : IRequestHandler<GetInsuranceReportQuery, Result<InsuranceReportFileResponse>>
{
    public async Task<Result<InsuranceReportFileResponse>> Handle(GetInsuranceReportQuery request, CancellationToken cancellationToken)
    {
        var generatedAt = timeProvider.GetUtcNow();
        var rows = await reportingRepository.GetInsuranceReportItemsAsync(request.LocationId, cancellationToken).ConfigureAwait(false);
        var content = BuildCsv(rows, generatedAt);
        var fileName = $"insurance-report-{generatedAt.UtcDateTime:yyyy-MM-dd}.csv";
        return Result<InsuranceReportFileResponse>.Success(new InsuranceReportFileResponse(fileName, Encoding.UTF8.GetBytes(content)));
    }

    private static string BuildCsv(IReadOnlyList<InsuranceReportItem> rows, DateTimeOffset generatedAt)
    {
        var builder = new StringBuilder();
        builder.Append("# Insurance Inventory Report - Generated ")
            .AppendLine(generatedAt.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture));

        AppendCsvRow(builder, "Name", "Brand", "Category", "Serial Number", "Purchase Date", "Price", "Location", "Warranty Expiry");

        foreach (var row in rows)
        {
            AppendCsvRow(
                builder,
                row.Name,
                row.Brand,
                row.Category,
                row.SerialNumber,
                FormatDate(row.PurchaseDate),
                FormatAmount(row.Price),
                row.Location,
                FormatDate(row.WarrantyExpiry));
        }

        var total = rows.Sum(row => row.Price ?? 0m);
        AppendCsvRow(builder, "TOTAL", null, null, null, null, FormatAmount(total), null, null);
        return builder.ToString();
    }

    private static void AppendCsvRow(StringBuilder builder, params string?[] values)
    {
        builder.AppendLine(string.Join(",", values.Select(EscapeCsv)));
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"");
        return escaped.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{escaped}\"" : escaped;
    }

    private static string? FormatDate(DateOnly? value)
        => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatAmount(decimal value)
        => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string? FormatAmount(decimal? value)
        => value.HasValue ? FormatAmount(value.Value) : null;
}

public sealed class GetInsuranceReportQueryValidator : AbstractValidator<GetInsuranceReportQuery>
{
    public GetInsuranceReportQueryValidator()
    {
        RuleFor(query => query.LocationId)
            .Must(ValidationRules.BeValidOptionalGuid)
            .WithMessage("LocationId must be a non-empty GUID when provided.");
    }
}
