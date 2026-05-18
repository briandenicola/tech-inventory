using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Configurations;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        builder.Property(entity => entity.EntityType)
            .HasMaxLength(200)
            .UseCollation("NOCASE")
            .IsRequired();

        builder.Property(entity => entity.EntityId)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(entity => entity.Actor)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(entity => entity.BeforePayload)
            .HasMaxLength(32768)
            .IsRequired();

        builder.Property(entity => entity.AfterPayload)
            .HasMaxLength(32768)
            .IsRequired();

        builder.Property(entity => entity.Timestamp)
            .IsRequired();

        builder.HasIndex(entity => new { entity.EntityType, entity.EntityId, entity.Timestamp });
        builder.HasIndex(entity => entity.Timestamp);
    }
}
