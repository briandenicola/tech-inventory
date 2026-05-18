using FluentValidation;
using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Common.Validation;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Exports;

public enum ExportFormat
{
    Csv = 1,
    Json = 2,
}

public sealed record DeviceExportRow(
    Guid Id,
    string Name,
    string? Brand,
    string Category,
    string Owner,
    string Location,
    string? Network,
    string? Model,
    string? SerialNumber,
    DateOnly? PurchaseDate,
    decimal? PurchasePrice,
    string CurrencyCode,
    string Status,
    string? Notes,
    DateOnly? RetiredDate,
    string? DisposalMethod,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset ModifiedAt,
    string? ModifiedBy);

public sealed record ExportDevicesQuery(
    ExportFormat Format = ExportFormat.Csv,
    string? Search = null,
    Guid? BrandId = null,
    Guid? CategoryId = null,
    Guid? OwnerId = null,
    Guid? LocationId = null,
    Guid? NetworkId = null,
    DeviceStatus? Status = null,
    IReadOnlyCollection<Guid>? TagIds = null,
    int? PurchaseYearFrom = null,
    int? PurchaseYearTo = null,
    string? SortBy = null,
    bool SortDescending = false) : IRequest<Result<IAsyncEnumerable<DeviceExportRow>>>;

public sealed class ExportDevicesQueryHandler(IDeviceExportService deviceExportService)
    : IRequestHandler<ExportDevicesQuery, Result<IAsyncEnumerable<DeviceExportRow>>>
{
    public Task<Result<IAsyncEnumerable<DeviceExportRow>>> Handle(ExportDevicesQuery request, CancellationToken cancellationToken)
    {
        var criteria = new DeviceListCriteria(
            new PageRequest(1, 200),
            request.Search,
            request.BrandId,
            request.CategoryId,
            request.OwnerId,
            request.LocationId,
            request.NetworkId,
            request.Status,
            request.TagIds,
            request.PurchaseYearFrom.HasValue ? new DateOnly(request.PurchaseYearFrom.Value, 1, 1) : null,
            request.PurchaseYearTo.HasValue ? new DateOnly(request.PurchaseYearTo.Value, 12, 31) : null,
            request.SortBy,
            request.SortDescending);

        return Task.FromResult(Result<IAsyncEnumerable<DeviceExportRow>>.Success(deviceExportService.StreamExportAsync(criteria)));
    }
}

public sealed class ExportDevicesQueryValidator : AbstractValidator<ExportDevicesQuery>
{
    public ExportDevicesQueryValidator()
    {
        RuleFor(query => query.BrandId)
            .Must(ValidationRules.BeValidOptionalGuid)
            .WithMessage("BrandId must be a non-empty GUID when provided.");

        RuleFor(query => query.CategoryId)
            .Must(ValidationRules.BeValidOptionalGuid)
            .WithMessage("CategoryId must be a non-empty GUID when provided.");

        RuleFor(query => query.OwnerId)
            .Must(ValidationRules.BeValidOptionalGuid)
            .WithMessage("OwnerId must be a non-empty GUID when provided.");

        RuleFor(query => query.LocationId)
            .Must(ValidationRules.BeValidOptionalGuid)
            .WithMessage("LocationId must be a non-empty GUID when provided.");

        RuleFor(query => query.NetworkId)
            .Must(ValidationRules.BeValidOptionalGuid)
            .WithMessage("NetworkId must be a non-empty GUID when provided.");

        RuleFor(query => query.PurchaseYearFrom)
            .InclusiveBetween(1, 9999)
            .When(query => query.PurchaseYearFrom.HasValue);

        RuleFor(query => query.PurchaseYearTo)
            .InclusiveBetween(1, 9999)
            .When(query => query.PurchaseYearTo.HasValue);

        RuleFor(query => query)
            .Must(query => !query.PurchaseYearFrom.HasValue || !query.PurchaseYearTo.HasValue || query.PurchaseYearFrom <= query.PurchaseYearTo)
            .WithMessage("PurchaseYearFrom cannot be greater than PurchaseYearTo.");

        RuleFor(query => query.SortBy)
            .Must(sortBy => ValidationRules.BeValidSort(sortBy, "name", "purchaseDate", "createdAt"))
            .WithMessage("SortBy must be one of: name, purchaseDate, createdAt.");

        When(query => query.TagIds is not null, () =>
        {
            RuleForEach(query => query.TagIds!)
                .NotEmpty()
                .WithMessage("TagIds cannot contain empty GUID values.");
        });
    }
}
