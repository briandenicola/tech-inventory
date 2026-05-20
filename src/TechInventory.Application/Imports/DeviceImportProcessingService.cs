using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using FluentValidation;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Validation;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Imports;

public sealed record ImportProcessingResult(
    int TotalRows,
    IReadOnlyList<PreparedImportRow> ValidRows,
    IReadOnlyList<ImportRowError> InvalidRows,
    IReadOnlyList<MissingLookup> LookupsToCreate);

public sealed record PreparedImportRow(
    int RowNumber,
    IReadOnlyDictionary<string, string?> RawValues,
    ImportDevicePreview Device,
    Guid? BrandId,
    Guid? CategoryId,
    Guid? OwnerId,
    Guid? LocationId,
    Guid? NetworkId);

public interface IDeviceImportProcessingService
{
    Task<ImportProcessingResult> ProcessAsync(byte[] fileContents, IReadOnlyDictionary<string, string>? columnMapping, CancellationToken cancellationToken);

    string? SerializeErrorLog(IReadOnlyList<ImportRowError> errors);
}

internal sealed class DeviceImportProcessingService(
    IBrandRepository brandRepository,
    ICategoryRepository categoryRepository,
    IOwnerRepository ownerRepository,
    ILocationRepository locationRepository,
    INetworkRepository networkRepository) : IDeviceImportProcessingService
{
    private const int MaxStoredErrorLogLength = 32_768;

    private static readonly CsvConfiguration CsvConfiguration = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        MissingFieldFound = null,
        BadDataFound = null,
        PrepareHeaderForMatch = args => args.Header?.Trim() ?? string.Empty,
    };

    private static readonly ImportDeviceCandidateValidator CandidateValidator = new();

    public async Task<ImportProcessingResult> ProcessAsync(
        byte[] fileContents,
        IReadOnlyDictionary<string, string>? columnMapping,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fileContents);

        var lookupCatalog = await BuildLookupCatalogAsync(cancellationToken).ConfigureAwait(false);
        using var stream = new MemoryStream(fileContents, writable: false);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CsvConfiguration);

        if (!await csv.ReadAsync().ConfigureAwait(false))
        {
            return new ImportProcessingResult(0, [], [], []);
        }

        csv.ReadHeader();
        var headers = csv.HeaderRecord?
            .Where(header => !string.IsNullOrWhiteSpace(header))
            .Select(header => header.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        var headerMappings = BuildHeaderMappings(headers, columnMapping);
        var validRows = new List<PreparedImportRow>();
        var invalidRows = new List<ImportRowError>();
        var missingLookups = new List<MissingLookup>();
        var totalRows = 0;

        while (await csv.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            totalRows++;

            var rowNumber = csv.Context.Parser?.Row ?? (totalRows + 1);
            var rawValues = ReadRawValues(csv, headers);
            var fieldValues = BuildFieldValues(rawValues, headerMappings);
            var rowErrors = new List<ImportFieldError>();

            var purpose = Normalize(fieldValues, ImportFieldNames.Purpose);
            var purchaseDate = ParseDate(fieldValues, ImportFieldNames.PurchaseDate, rowErrors);
            var retiredDate = ParseDate(fieldValues, ImportFieldNames.RetiredDate, rowErrors);
            var status = ParseSharePointStatus(fieldValues, purpose, ref retiredDate, purchaseDate, rowErrors, out var disposalMethod);
            var purchasePrice = ParseDecimal(fieldValues, ImportFieldNames.PurchasePrice, rowErrors);
            var network = NormalizeNetworking(fieldValues);
            var macAddress = NormalizeMacAddress(Normalize(fieldValues, ImportFieldNames.MacAddress), rowErrors);
            var productUrl = NormalizeProductUrl(Normalize(fieldValues, ImportFieldNames.ProductUrl), rowErrors);

            var candidate = new ImportDeviceCandidate(
                Normalize(fieldValues, ImportFieldNames.Name) ?? string.Empty,
                Normalize(fieldValues, ImportFieldNames.Brand) ?? string.Empty,
                Normalize(fieldValues, ImportFieldNames.Category) ?? string.Empty,
                Normalize(fieldValues, ImportFieldNames.Owner) ?? string.Empty,
                Normalize(fieldValues, ImportFieldNames.Location) ?? string.Empty,
                network,
                Normalize(fieldValues, ImportFieldNames.Model),
                Normalize(fieldValues, ImportFieldNames.SerialNumber),
                purchaseDate,
                purchasePrice,
                Normalize(fieldValues, ImportFieldNames.CurrencyCode),
                status,
                Normalize(fieldValues, ImportFieldNames.Notes),
                retiredDate,
                disposalMethod,
                purpose,
                Normalize(fieldValues, ImportFieldNames.OperatingSystem),
                Normalize(fieldValues, ImportFieldNames.IpAddress),
                macAddress,
                productUrl,
                Normalize(fieldValues, ImportFieldNames.Version));

            var validationResult = CandidateValidator.Validate(candidate);
            rowErrors.AddRange(validationResult.Errors.Select(error => new ImportFieldError(error.PropertyName, error.ErrorMessage)));

            if (rowErrors.Count == 0)
            {
                var rowMissingLookups = new List<MissingLookup>();
                var brandId = ResolveLookup(
                    entityType: nameof(Brand),
                    fieldName: ImportFieldNames.Brand,
                    value: candidate.Brand,
                    lookupCatalog.ActiveBrands,
                    lookupCatalog.InactiveBrands,
                    brand => brand.Id,
                    rowErrors,
                    rowMissingLookups,
                    allowCreate: true);
                var categoryId = ResolveLookup(
                    entityType: nameof(Category),
                    fieldName: ImportFieldNames.Category,
                    value: candidate.Category,
                    lookupCatalog.ActiveCategories,
                    lookupCatalog.InactiveCategories,
                    category => category.Id,
                    rowErrors,
                    rowMissingLookups,
                    allowCreate: true);
                var ownerId = ResolveLookup(
                    entityType: nameof(Owner),
                    fieldName: ImportFieldNames.Owner,
                    value: candidate.Owner,
                    lookupCatalog.ActiveOwners,
                    lookupCatalog.InactiveOwners,
                    owner => owner.Id,
                    rowErrors,
                    rowMissingLookups,
                    allowCreate: true);
                var locationId = ResolveLookup(
                    entityType: nameof(Location),
                    fieldName: ImportFieldNames.Location,
                    value: candidate.Location,
                    lookupCatalog.ActiveLocations,
                    lookupCatalog.InactiveLocations,
                    location => location.Id,
                    rowErrors,
                    rowMissingLookups,
                    allowCreate: true);
                var networkId = ResolveLookup(
                    entityType: nameof(Network),
                    fieldName: ImportFieldNames.Network,
                    value: candidate.Network,
                    lookupCatalog.ActiveNetworks,
                    lookupCatalog.InactiveNetworks,
                    network => network.Id,
                    rowErrors,
                    rowMissingLookups,
                    allowCreate: true);

                if (rowErrors.Count == 0)
                {
                    missingLookups.AddRange(rowMissingLookups);
                    validRows.Add(new PreparedImportRow(
                        rowNumber,
                        rawValues,
                        new ImportDevicePreview(
                            candidate.Name,
                            candidate.Brand,
                            candidate.Category,
                            candidate.Owner,
                            candidate.Location,
                            candidate.Model,
                            candidate.SerialNumber,
                            candidate.Network,
                            candidate.PurchaseDate,
                            candidate.PurchasePrice,
                            candidate.CurrencyCode,
                            candidate.Status.ToString(),
                            candidate.Notes,
                            candidate.RetiredDate,
                            candidate.DisposalMethod,
                            candidate.Purpose,
                            candidate.OperatingSystem,
                            candidate.IpAddress,
                            candidate.MacAddress,
                            candidate.ProductUrl,
                            candidate.Version),
                        brandId,
                        categoryId,
                        ownerId,
                        locationId,
                        networkId));
                }
            }

            if (rowErrors.Count > 0)
            {
                invalidRows.Add(new ImportRowError(rowNumber, rawValues, rowErrors));
            }
        }

        return new ImportProcessingResult(
            totalRows,
            validRows,
            invalidRows,
            missingLookups
                .DistinctBy(lookup => $"{lookup.EntityType}:{lookup.Name}", StringComparer.OrdinalIgnoreCase)
                .OrderBy(lookup => lookup.EntityType, StringComparer.OrdinalIgnoreCase)
                .ThenBy(lookup => lookup.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    public string? SerializeErrorLog(IReadOnlyList<ImportRowError> errors)
    {
        if (errors.Count == 0)
        {
            return null;
        }

        var storedErrors = errors.ToArray();
        var omittedErrorCount = 0;
        while (true)
        {
            var payload = new ImportBatchDetailResponse.StoredImportErrorLog(storedErrors, omittedErrorCount > 0, omittedErrorCount);
            var serialized = JsonSerializer.Serialize(payload, DeviceImportSerialization.SerializerOptions);
            if (serialized.Length <= MaxStoredErrorLogLength)
            {
                return serialized;
            }

            if (storedErrors.Length == 0)
            {
                return JsonSerializer.Serialize(
                    new ImportBatchDetailResponse.StoredImportErrorLog([], true, errors.Count),
                    DeviceImportSerialization.SerializerOptions);
            }

            storedErrors = storedErrors[..^1];
            omittedErrorCount++;
        }
    }

    private static Dictionary<string, string> BuildHeaderMappings(
        IReadOnlyCollection<string> headers,
        IReadOnlyDictionary<string, string>? columnMapping)
    {
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var providedMapping = columnMapping is null
            ? null
            : new Dictionary<string, string>(columnMapping, StringComparer.OrdinalIgnoreCase);

        foreach (var header in headers)
        {
            if (providedMapping is not null && providedMapping.TryGetValue(header, out var mappedField))
            {
                mappings[header] = ImportFieldNames.NormalizeRequired(mappedField);
                continue;
            }

            if (ImportFieldNames.TryNormalize(header, out var inferredField))
            {
                mappings[header] = inferredField;
            }
        }

        return mappings;
    }

    private static Dictionary<string, string?> BuildFieldValues(
        IReadOnlyDictionary<string, string?> rawValues,
        IReadOnlyDictionary<string, string> headerMappings)
    {
        var fieldValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in headerMappings)
        {
            if (rawValues.TryGetValue(mapping.Key, out var value) && !fieldValues.ContainsKey(mapping.Value))
            {
                fieldValues[mapping.Value] = value;
            }
        }

        return fieldValues;
    }

    private static Dictionary<string, string?> ReadRawValues(CsvReader csv, IReadOnlyList<string> headers)
    {
        var rawValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < headers.Count; index++)
        {
            rawValues[headers[index]] = csv.GetField(index);
        }

        return rawValues;
    }

    private static string? Normalize(IReadOnlyDictionary<string, string?> values, string fieldName)
        => values.TryGetValue(fieldName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;

    private static DeviceStatus ParseSharePointStatus(
        IReadOnlyDictionary<string, string?> values,
        string? purpose,
        ref DateOnly? retiredDate,
        DateOnly? purchaseDate,
        ICollection<ImportFieldError> errors,
        out string? disposalMethod)
    {
        disposalMethod = null;
        var rawRetired = Normalize(values, ImportFieldNames.Status);
        if (string.IsNullOrWhiteSpace(rawRetired))
        {
            return DeviceStatus.Active;
        }

        // Try to parse as enum (generic format: "Active", "Retired", "Disposed")
        if (Enum.TryParse<DeviceStatus>(rawRetired, ignoreCase: true, out var parsedStatus))
        {
            if (parsedStatus == DeviceStatus.Disposed && !string.IsNullOrWhiteSpace(purpose))
            {
                disposalMethod = purpose;
            }
            if (parsedStatus is DeviceStatus.Retired or DeviceStatus.Disposed)
            {
                retiredDate ??= purchaseDate;
            }
            return parsedStatus;
        }

        // Try to parse as boolean (SharePoint format: "True", "False")
        if (!bool.TryParse(rawRetired, out var isRetired))
        {
            errors.Add(new ImportFieldError(ImportFieldNames.Status, $"Status '{rawRetired}' must be Active, Retired, Disposed, True, or False."));
            return DeviceStatus.Active;
        }

        if (!isRetired)
        {
            return DeviceStatus.Active;
        }

        if (!string.IsNullOrWhiteSpace(purpose) &&
            System.Text.RegularExpressions.Regex.IsMatch(purpose, @"\b(sold|given|donated|gifted|disposed|trashed)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            disposalMethod = purpose;
            return DeviceStatus.Disposed;
        }

        retiredDate ??= purchaseDate;
        return DeviceStatus.Retired;
    }

    private static string? NormalizeNetworking(IReadOnlyDictionary<string, string?> values)
    {
        var rawNetwork = Normalize(values, ImportFieldNames.Network);
        return string.Equals(rawNetwork, "N/A", StringComparison.OrdinalIgnoreCase)
            ? null
            : rawNetwork;
    }

    private static string? NormalizeMacAddress(string? macAddress, ICollection<ImportFieldError> errors)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            return null;
        }

        var cleaned = macAddress.Replace(":", "").Replace("-", "").Replace(".", "").Replace(" ", "").ToUpperInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(cleaned, @"^[0-9A-F]{12}$"))
        {
            errors.Add(new ImportFieldError(ImportFieldNames.MacAddress, $"MAC Address '{macAddress}' is not valid (expected 12 hex digits)."));
            return null;
        }

        return $"{cleaned[0]}{cleaned[1]}:{cleaned[2]}{cleaned[3]}:{cleaned[4]}{cleaned[5]}:{cleaned[6]}{cleaned[7]}:{cleaned[8]}{cleaned[9]}:{cleaned[10]}{cleaned[11]}";
    }

    private static string? NormalizeProductUrl(string? url, ICollection<ImportFieldError> errors)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsedUri) || (parsedUri.Scheme != "http" && parsedUri.Scheme != "https"))
        {
            errors.Add(new ImportFieldError(ImportFieldNames.ProductUrl, $"URL '{url}' is not a valid absolute HTTP/HTTPS URL."));
            return null;
        }

        return url;
    }

    private static DeviceStatus ParseStatus(IReadOnlyDictionary<string, string?> values, ICollection<ImportFieldError> errors)
    {
        var rawStatus = Normalize(values, ImportFieldNames.Status);
        if (string.IsNullOrWhiteSpace(rawStatus))
        {
            return DeviceStatus.Active;
        }

        var normalizedStatus = string.Concat(rawStatus.Where(char.IsLetterOrDigit));
        if (Enum.TryParse<DeviceStatus>(normalizedStatus, ignoreCase: true, out var parsedStatus))
        {
            return parsedStatus;
        }

        errors.Add(new ImportFieldError(ImportFieldNames.Status, $"Status '{rawStatus}' is not valid."));
        return DeviceStatus.Active;
    }

    private static DateOnly? ParseDate(IReadOnlyDictionary<string, string?> values, string fieldName, ICollection<ImportFieldError> errors)
    {
        var rawValue = Normalize(values, fieldName);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (DateOnly.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedDate)
            || DateOnly.TryParse(rawValue, out parsedDate))
        {
            return parsedDate;
        }

        errors.Add(new ImportFieldError(fieldName, $"{fieldName} '{rawValue}' is not a valid date."));
        return null;
    }

    private static decimal? ParseDecimal(IReadOnlyDictionary<string, string?> values, string fieldName, ICollection<ImportFieldError> errors)
    {
        var rawValue = Normalize(values, fieldName);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out var parsedDecimal)
            || decimal.TryParse(rawValue, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.CurrentCulture, out parsedDecimal))
        {
            return parsedDecimal;
        }

        errors.Add(new ImportFieldError(fieldName, $"{fieldName} '{rawValue}' is not a valid decimal value."));
        return null;
    }

    private static Guid? ResolveLookup<TLookup>(
        string entityType,
        string fieldName,
        string? value,
        IReadOnlyDictionary<string, IReadOnlyList<TLookup>> activeLookup,
        IReadOnlyDictionary<string, IReadOnlyList<TLookup>> inactiveLookup,
        Func<TLookup, Guid> idSelector,
        ICollection<ImportFieldError> errors,
        ICollection<MissingLookup> missingLookups,
        bool allowCreate)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (activeLookup.TryGetValue(value, out var activeMatches))
        {
            if (activeMatches.Count == 1)
            {
                return idSelector(activeMatches[0]);
            }

            errors.Add(new ImportFieldError(fieldName, $"{entityType} '{value}' is ambiguous. Use a unique name before importing."));
            return null;
        }

        if (inactiveLookup.ContainsKey(value))
        {
            errors.Add(new ImportFieldError(fieldName, $"{entityType} '{value}' exists but is inactive."));
            return null;
        }

        if (allowCreate)
        {
            missingLookups.Add(new MissingLookup(entityType, value));
            return null;
        }

        errors.Add(new ImportFieldError(fieldName, $"{entityType} '{value}' was not found."));
        return null;
    }

    private async Task<LookupCatalog> BuildLookupCatalogAsync(CancellationToken cancellationToken)
    {
        var brands = await brandRepository.ListAsync(true, cancellationToken).ConfigureAwait(false);
        var categories = await categoryRepository.ListAsync(true, cancellationToken).ConfigureAwait(false);
        var owners = await ownerRepository.ListAsync(true, cancellationToken).ConfigureAwait(false);
        var locations = await locationRepository.ListAsync(true, cancellationToken).ConfigureAwait(false);
        var networks = await networkRepository.ListAsync(true, cancellationToken).ConfigureAwait(false);

        return new LookupCatalog(
            CreateLookup(brands.Where(brand => brand.IsActive), brand => brand.Name),
            CreateLookup(brands.Where(brand => !brand.IsActive), brand => brand.Name),
            CreateLookup(categories.Where(category => category.IsActive), category => category.Name),
            CreateLookup(categories.Where(category => !category.IsActive), category => category.Name),
            CreateLookup(owners.Where(owner => owner.IsActive), owner => owner.DisplayName),
            CreateLookup(owners.Where(owner => !owner.IsActive), owner => owner.DisplayName),
            CreateLookup(locations.Where(location => location.IsActive), location => location.Name),
            CreateLookup(locations.Where(location => !location.IsActive), location => location.Name),
            CreateLookup(networks.Where(network => network.IsActive), network => network.Name),
            CreateLookup(networks.Where(network => !network.IsActive), network => network.Name));
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<TLookup>> CreateLookup<TLookup>(
        IEnumerable<TLookup> items,
        Func<TLookup, string> keySelector)
    {
        return items
            .GroupBy(item => keySelector(item), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<TLookup>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    private sealed record ImportDeviceCandidate(
        string Name,
        string Brand,
        string Category,
        string Owner,
        string Location,
        string? Network,
        string? Model,
        string? SerialNumber,
        DateOnly? PurchaseDate,
        decimal? PurchasePrice,
        string? CurrencyCode,
        DeviceStatus Status,
        string? Notes,
        DateOnly? RetiredDate,
        string? DisposalMethod,
        string? Purpose,
        string? OperatingSystem,
        string? IpAddress,
        string? MacAddress,
        string? ProductUrl,
        string? Version);

    private sealed class ImportDeviceCandidateValidator : AbstractValidator<ImportDeviceCandidate>
    {
        public ImportDeviceCandidateValidator()
        {
            DeviceValidationRules.ApplySharedDeviceRules(
                this,
                row => row.Name,
                row => row.CurrencyCode,
                row => row.Model,
                row => row.SerialNumber,
                row => row.PurchaseDate,
                row => row.PurchasePrice,
                row => row.Notes,
                row => row.DisposalMethod,
                row => row.RetiredDate,
                row => null,
                row => row.Status);

            DeviceValidationRules.ApplyExtendedFieldRules(
                this,
                row => row.Purpose,
                row => row.OperatingSystem,
                row => row.IpAddress,
                row => row.MacAddress,
                row => row.ProductUrl,
                row => row.Version);

            RuleFor(row => row.Brand)
                .MaximumLength(200);

            RuleFor(row => row.Category)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(row => row.Owner)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(row => row.Location)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(row => row.Network)
                .MaximumLength(200);
        }
    }

    private sealed record LookupCatalog(
        IReadOnlyDictionary<string, IReadOnlyList<Brand>> ActiveBrands,
        IReadOnlyDictionary<string, IReadOnlyList<Brand>> InactiveBrands,
        IReadOnlyDictionary<string, IReadOnlyList<Category>> ActiveCategories,
        IReadOnlyDictionary<string, IReadOnlyList<Category>> InactiveCategories,
        IReadOnlyDictionary<string, IReadOnlyList<Owner>> ActiveOwners,
        IReadOnlyDictionary<string, IReadOnlyList<Owner>> InactiveOwners,
        IReadOnlyDictionary<string, IReadOnlyList<Location>> ActiveLocations,
        IReadOnlyDictionary<string, IReadOnlyList<Location>> InactiveLocations,
        IReadOnlyDictionary<string, IReadOnlyList<Network>> ActiveNetworks,
        IReadOnlyDictionary<string, IReadOnlyList<Network>> InactiveNetworks);
}
