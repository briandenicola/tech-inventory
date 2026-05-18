using System.Text.Json;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Imports;

public sealed record PreviewImportResult(
    int TotalRows,
    IReadOnlyList<ImportRowPreview> ValidRows,
    IReadOnlyList<ImportRowError> InvalidRows,
    IReadOnlyList<MissingLookup> LookupsToCreate);

public sealed record CommitImportResult(
    Guid BatchId,
    int TotalRows,
    int ImportedRows,
    int InvalidRows,
    IReadOnlyList<ImportRowError> FailedRows);

public sealed record ImportRowPreview(
    int RowNumber,
    ImportDevicePreview Device,
    Guid? BrandId,
    Guid? CategoryId,
    Guid? OwnerId,
    Guid? LocationId,
    Guid? NetworkId);

public sealed record ImportDevicePreview(
    string Name,
    string Brand,
    string Category,
    string Owner,
    string Location,
    string? Model,
    string? SerialNumber,
    string? Network,
    DateOnly? PurchaseDate,
    decimal? PurchasePrice,
    string? CurrencyCode,
    string Status,
    string? Notes,
    DateOnly? RetiredDate,
    string? DisposalMethod);

public sealed record ImportRowError(
    int RowNumber,
    IReadOnlyDictionary<string, string?> RawValues,
    IReadOnlyList<ImportFieldError> Errors);

public sealed record ImportFieldError(string Field, string Message);

public sealed record MissingLookup(string EntityType, string Name);

public sealed record ImportBatchSummaryResponse(
    Guid Id,
    string FileName,
    string? ImportedBy,
    int RowCount,
    int SuccessCount,
    int ErrorCount,
    string Status,
    DateTimeOffset CreatedAt)
{
    public static ImportBatchSummaryResponse FromEntity(ImportBatch importBatch)
    {
        ArgumentNullException.ThrowIfNull(importBatch);

        return new ImportBatchSummaryResponse(
            importBatch.Id,
            importBatch.FileName,
            importBatch.ImportedBy,
            importBatch.RowCount,
            importBatch.SuccessCount,
            importBatch.ErrorCount,
            importBatch.Status.ToString(),
            importBatch.CreatedAt);
    }
}

public sealed record ImportBatchDetailResponse(
    Guid Id,
    string FileName,
    string? ImportedBy,
    int RowCount,
    int SuccessCount,
    int ErrorCount,
    string Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ImportRowError> Errors,
    bool IsErrorLogTruncated,
    int OmittedErrorCount,
    string? ErrorLog)
{
    public static ImportBatchDetailResponse FromEntity(ImportBatch importBatch)
    {
        ArgumentNullException.ThrowIfNull(importBatch);

        var parsedErrorLog = ParseErrorLog(importBatch.ErrorLog);
        return new ImportBatchDetailResponse(
            importBatch.Id,
            importBatch.FileName,
            importBatch.ImportedBy,
            importBatch.RowCount,
            importBatch.SuccessCount,
            importBatch.ErrorCount,
            importBatch.Status.ToString(),
            importBatch.CreatedAt,
            parsedErrorLog.Errors,
            parsedErrorLog.IsTruncated,
            parsedErrorLog.OmittedErrorCount,
            importBatch.ErrorLog);
    }

    private static ParsedImportErrorLog ParseErrorLog(string? errorLog)
    {
        if (string.IsNullOrWhiteSpace(errorLog))
        {
            return ParsedImportErrorLog.Empty;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<StoredImportErrorLog>(errorLog, DeviceImportSerialization.SerializerOptions);
            if (parsed is null)
            {
                return ParsedImportErrorLog.Empty;
            }

            return new ParsedImportErrorLog(
                parsed.Errors ?? [],
                parsed.IsTruncated,
                parsed.OmittedErrorCount);
        }
        catch (JsonException)
        {
            return ParsedImportErrorLog.Empty;
        }
    }

    private sealed record ParsedImportErrorLog(
        IReadOnlyList<ImportRowError> Errors,
        bool IsTruncated,
        int OmittedErrorCount)
    {
        public static ParsedImportErrorLog Empty { get; } = new([], false, 0);
    }

    internal sealed record StoredImportErrorLog(
        IReadOnlyList<ImportRowError>? Errors,
        bool IsTruncated,
        int OmittedErrorCount);
}

internal static class DeviceImportSerialization
{
    public static JsonSerializerOptions SerializerOptions { get; } = new(JsonSerializerDefaults.Web);
}
