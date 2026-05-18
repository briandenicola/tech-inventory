using FluentAssertions;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.UnitTests.Domain;

public class ImportBatchContractTests
{
    [Fact]
    public void ImportBatch_RejectsNegativeCounts()
    {
        var act = () => new ImportBatch(Guid.NewGuid(), "devices.csv", -1, 0, 0, ImportStatus.Pending, DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ImportBatch_RejectsProcessedCountsThatExceedTheRowCount()
    {
        var act = () => new ImportBatch(Guid.NewGuid(), "devices.csv", 3, 2, 2, ImportStatus.PartialSuccess, DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ImportBatch_CapturesCreationSummaryAndDerivedFlags()
    {
        var createdAt = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var batch = new ImportBatch(
            Guid.NewGuid(),
            " devices.csv ",
            10,
            8,
            2,
            ImportStatus.PartialSuccess,
            createdAt,
            importedBy: "  hicks  ",
            errorLog: "  [{\"row\":9}]  ");

        batch.FileName.Should().Be("devices.csv");
        batch.ImportedBy.Should().Be("hicks");
        batch.Status.Should().Be(ImportStatus.PartialSuccess);
        batch.RowCount.Should().Be(10);
        batch.SuccessCount.Should().Be(8);
        batch.ErrorCount.Should().Be(2);
        batch.ProcessedCount.Should().Be(10);
        batch.HasErrors.Should().BeTrue();
        batch.CreatedAt.Should().Be(createdAt);
        batch.ErrorLog.Should().Be("[{\"row\":9}]");
    }

    [Fact]
    public void ImportBatch_ExposesNoWritablePublicPropertiesOrMutators()
    {
        var publicSetters = typeof(ImportBatch)
            .GetProperties()
            .Where(property => property.SetMethod is { IsPublic: true })
            .Select(property => property.Name)
            .ToArray();

        var publicDeclaredMethods = typeof(ImportBatch)
            .GetMethods()
            .Where(method => method.IsPublic && !method.IsSpecialName && method.DeclaringType == typeof(ImportBatch))
            .Select(method => method.Name)
            .ToArray();

        publicSetters.Should().BeEmpty();
        publicDeclaredMethods.Should().BeEmpty();
    }
}
