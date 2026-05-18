using FluentValidation;
using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Abstractions.Services;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Devices;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.Domain.ValueObjects;

namespace TechInventory.Application.Imports;

public sealed record CommitImportCommand(
    byte[] FileContents,
    string? FileName = null,
    IReadOnlyDictionary<string, string>? ColumnMapping = null) : IRequest<Result<CommitImportResult>>, IAuditable;

public sealed class CommitImportCommandHandler(
    IDeviceImportProcessingService importProcessingService,
    IBrandRepository brandRepository,
    ICategoryRepository categoryRepository,
    IOwnerRepository ownerRepository,
    ILocationRepository locationRepository,
    IDeviceRepository deviceRepository,
    IImportBatchRepository importBatchRepository,
    IHouseholdRepository householdRepository,
    ICurrentUserService currentUserService,
    IAuditContext auditContext)
    : IRequestHandler<CommitImportCommand, Result<CommitImportResult>>
{
    public async Task<Result<CommitImportResult>> Handle(CommitImportCommand request, CancellationToken cancellationToken)
    {
        var preview = await importProcessingService.ProcessAsync(request.FileContents, request.ColumnMapping, cancellationToken).ConfigureAwait(false);
        var householdResult = await ResolveSingleHouseholdAsync(cancellationToken).ConfigureAwait(false);
        if (householdResult.IsFailure)
        {
            return Result<CommitImportResult>.Failure(householdResult.Error!);
        }

        var createdBrands = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var createdCategories = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var createdOwners = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var createdLocations = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var missingLookup in preview.LookupsToCreate)
        {
            switch (missingLookup.EntityType)
            {
                case nameof(Brand):
                    {
                        var brand = new Brand(Guid.NewGuid(), missingLookup.Name);
                        var addBrandResult = await brandRepository.AddAsync(brand, cancellationToken).ConfigureAwait(false);
                        if (addBrandResult.IsFailure)
                        {
                            return Result<CommitImportResult>.Failure(addBrandResult.Error!);
                        }

                        createdBrands[missingLookup.Name] = brand.Id;
                        break;
                    }
                case nameof(Category):
                    {
                        var category = new Category(Guid.NewGuid(), missingLookup.Name);
                        var addCategoryResult = await categoryRepository.AddAsync(category, cancellationToken).ConfigureAwait(false);
                        if (addCategoryResult.IsFailure)
                        {
                            return Result<CommitImportResult>.Failure(addCategoryResult.Error!);
                        }

                        createdCategories[missingLookup.Name] = category.Id;
                        break;
                    }
                case nameof(Owner):
                    {
                        var owner = new Owner(Guid.NewGuid(), missingLookup.Name, OwnerRole.Member);
                        var addOwnerResult = await ownerRepository.AddAsync(owner, cancellationToken).ConfigureAwait(false);
                        if (addOwnerResult.IsFailure)
                        {
                            return Result<CommitImportResult>.Failure(addOwnerResult.Error!);
                        }

                        createdOwners[missingLookup.Name] = owner.Id;
                        break;
                    }
                case nameof(Location):
                    {
                        var location = new Location(Guid.NewGuid(), missingLookup.Name, LocationType.Home);
                        var addLocationResult = await locationRepository.AddAsync(location, cancellationToken).ConfigureAwait(false);
                        if (addLocationResult.IsFailure)
                        {
                            return Result<CommitImportResult>.Failure(addLocationResult.Error!);
                        }

                        createdLocations[missingLookup.Name] = location.Id;
                        break;
                    }
            }
        }

        var failedRows = preview.InvalidRows.ToList();
        var importedRows = 0;
        foreach (var row in preview.ValidRows)
        {
            try
            {
                var device = Device.Create(
                    Guid.NewGuid(),
                    householdResult.Value!,
                    row.Device.Name,
                    row.BrandId ?? createdBrands[row.Device.Brand],
                    row.CategoryId ?? createdCategories[row.Device.Category],
                    row.OwnerId ?? createdOwners[row.Device.Owner],
                    row.LocationId ?? createdLocations[row.Device.Location],
                    row.Device.Model,
                    row.Device.SerialNumber,
                    row.NetworkId,
                    row.Device.PurchaseDate,
                    row.Device.PurchasePrice,
                    string.IsNullOrWhiteSpace(row.Device.CurrencyCode) ? null : Currency.From(row.Device.CurrencyCode),
                    Enum.Parse<DeviceStatus>(row.Device.Status, ignoreCase: true),
                    row.Device.Notes,
                    row.Device.RetiredDate,
                    row.Device.DisposalMethod);

                var addDeviceResult = await deviceRepository.AddAsync(device, cancellationToken).ConfigureAwait(false);
                if (addDeviceResult.IsFailure)
                {
                    failedRows.Add(new ImportRowError(row.RowNumber, row.RawValues, [new ImportFieldError("row", addDeviceResult.Error!.Message)]));
                    continue;
                }

                auditContext.Add(new AuditContextEntry(
                    nameof(Device),
                    device.Id.ToString(),
                    AuditAction.Created,
                    afterPayload: DeviceResponse.FromEntity(device)));
                importedRows++;
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException or KeyNotFoundException)
            {
                failedRows.Add(new ImportRowError(row.RowNumber, row.RawValues, [new ImportFieldError("row", exception.Message)]));
            }
        }

        var importBatch = new ImportBatch(
            Guid.NewGuid(),
            string.IsNullOrWhiteSpace(request.FileName) ? "uploaded.csv" : request.FileName,
            preview.TotalRows,
            importedRows,
            failedRows.Count,
            DetermineStatus(importedRows, failedRows.Count),
            currentUserService.GetCurrentUserId(),
            importProcessingService.SerializeErrorLog(failedRows));

        var addBatchResult = await importBatchRepository.AddAsync(importBatch, cancellationToken).ConfigureAwait(false);
        if (addBatchResult.IsFailure)
        {
            return Result<CommitImportResult>.Failure(addBatchResult.Error!);
        }

        return Result<CommitImportResult>.Success(
            new CommitImportResult(
                importBatch.Id,
                preview.TotalRows,
                importedRows,
                failedRows.Count,
                failedRows));
    }

    private async Task<Result<Household>> ResolveSingleHouseholdAsync(CancellationToken cancellationToken)
    {
        var households = await householdRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        return households.Count switch
        {
            1 => Result<Household>.Success(households[0]),
            0 => Result<Household>.Failure(Error.Conflict("A household must exist before importing devices.")),
            _ => Result<Household>.Failure(Error.Conflict("CommitImportCommand requires exactly one household.")),
        };
    }

    private static ImportStatus DetermineStatus(int importedRows, int failedRows)
        => importedRows switch
        {
            0 when failedRows > 0 => ImportStatus.Failed,
            _ when failedRows > 0 => ImportStatus.PartialSuccess,
            _ => ImportStatus.Completed,
        };
}

public sealed class CommitImportCommandValidator : AbstractValidator<CommitImportCommand>
{
    public CommitImportCommandValidator()
    {
        RuleFor(command => command.FileContents)
            .Must(fileContents => fileContents is { Length: > 0 })
            .WithMessage("FileContents must not be empty.");

        RuleFor(command => command.FileName)
            .MaximumLength(512);

        When(command => command.ColumnMapping is not null, () =>
        {
            RuleForEach(command => command.ColumnMapping!)
                .Must(mapping => !string.IsNullOrWhiteSpace(mapping.Key) && !string.IsNullOrWhiteSpace(mapping.Value))
                .WithMessage("Column mappings must include both a CSV column name and a target field.");

            RuleFor(command => command.ColumnMapping!)
                .Must(mapping => mapping.Values.All(value => ImportFieldNames.TryNormalize(value, out _)))
                .WithMessage($"Column mappings must target supported fields: {string.Join(", ", ImportFieldNames.SupportedFields)}.");
        });
    }
}
