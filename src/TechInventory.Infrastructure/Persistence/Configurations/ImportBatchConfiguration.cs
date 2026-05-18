using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Configurations;

public sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.ToTable("ImportBatches");
        builder.HasKey(entity => entity.Id);
        builder.Ignore(entity => entity.ProcessedCount);
        builder.Ignore(entity => entity.HasErrors);

        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        builder.Property(entity => entity.FileName)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(entity => entity.ImportedBy)
            .HasMaxLength(256);

        builder.Property(entity => entity.RowCount)
            .IsRequired();

        builder.Property(entity => entity.SuccessCount)
            .IsRequired();

        builder.Property(entity => entity.ErrorCount)
            .IsRequired();

        builder.Property(entity => entity.Status)
            .IsRequired();

        builder.Property(entity => entity.ErrorLog)
            .HasMaxLength(32768);

        builder.Property(entity => entity.CreatedAt)
            .IsRequired();

        builder.HasIndex(entity => new { entity.Status, entity.CreatedAt });
    }
}
