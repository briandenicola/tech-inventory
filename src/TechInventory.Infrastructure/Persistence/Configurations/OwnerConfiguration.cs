using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Configurations;

public sealed class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    public void Configure(EntityTypeBuilder<Owner> builder)
    {
        builder.ToTable("Owners");
        builder.HasKey(entity => entity.Id);
        builder.Ignore(entity => entity.NormalizedDisplayName);

        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        builder.Property(entity => entity.DisplayName)
            .HasMaxLength(200)
            .UseCollation("NOCASE")
            .IsRequired();

        builder.Property(entity => entity.Role)
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .IsRequired();

        builder.Property(entity => entity.CreatedAt)
            .IsRequired();

        builder.Property(entity => entity.CreatedBy)
            .HasMaxLength(256);

        builder.Property(entity => entity.ModifiedAt)
            .IsRequired();

        builder.Property(entity => entity.ModifiedBy)
            .HasMaxLength(256);

        builder.HasIndex(entity => entity.DisplayName)
            .IsUnique();

        builder.HasIndex(entity => entity.EntraObjectId)
            .IsUnique();
    }
}
