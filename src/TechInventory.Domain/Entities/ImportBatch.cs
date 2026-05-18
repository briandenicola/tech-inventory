using TechInventory.Domain.Enums;
using TechInventory.Domain.Primitives;

namespace TechInventory.Domain.Entities;

public sealed class ImportBatch
{
    private ImportBatch()
    {
        Id = Guid.Empty;
        FileName = null!;
    }

    public ImportBatch(
        Guid id,
        string fileName,
        int rowCount,
        int successCount,
        int errorCount,
        ImportStatus status,
        DateTimeOffset createdAt,
        string? importedBy = null,
        string? errorLog = null)
    {
        Id = Guard.AgainstDefault(id, nameof(id));
        FileName = Guard.AgainstNullOrWhiteSpace(fileName, nameof(fileName), 512);
        ImportedBy = Guard.AgainstMaxLength(importedBy, nameof(importedBy), 256);
        RowCount = ValidateNonNegative(rowCount, nameof(rowCount));
        SuccessCount = ValidateNonNegative(successCount, nameof(successCount));
        ErrorCount = ValidateNonNegative(errorCount, nameof(errorCount));
        Status = status;
        ErrorLog = Guard.AgainstMaxLength(errorLog, nameof(errorLog), 32_768);
        CreatedAt = ValidateCreatedAt(createdAt);

        ValidateCounts(RowCount, SuccessCount, ErrorCount);
    }

    public ImportBatch(Guid id, string fileName, int rowCount, int successCount, int errorCount, ImportStatus status, string? importedBy = null, string? errorLog = null)
        : this(id, fileName, rowCount, successCount, errorCount, status, DateTimeOffset.UtcNow, importedBy, errorLog)
    {
    }

    public Guid Id { get; private set; }

    public string FileName { get; private set; }

    public string? ImportedBy { get; private set; }

    public int RowCount { get; private set; }

    public int SuccessCount { get; private set; }

    public int ErrorCount { get; private set; }

    public ImportStatus Status { get; private set; }

    public string? ErrorLog { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public int ProcessedCount => SuccessCount + ErrorCount;

    public bool HasErrors => ErrorCount > 0;

    private static DateTimeOffset ValidateCreatedAt(DateTimeOffset createdAt)
    {
        if (createdAt == default)
        {
            throw new ArgumentOutOfRangeException(nameof(createdAt), "createdAt must be provided.");
        }

        return createdAt;
    }

    private static int ValidateNonNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, $"{paramName} cannot be negative.");
        }

        return value;
    }

    private static void ValidateCounts(int rowCount, int successCount, int errorCount)
    {
        if (successCount + errorCount > rowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(successCount), "The sum of successCount and errorCount cannot exceed rowCount.");
        }
    }
}
