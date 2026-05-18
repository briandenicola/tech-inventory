using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Configurations;

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands");
        builder.HasKey(entity => entity.Id);
        builder.Ignore(entity => entity.NormalizedName);

        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        builder.Property(entity => entity.Name)
            .HasMaxLength(200)
            .UseCollation("NOCASE")
            .IsRequired();

        builder.Property(entity => entity.Website)
            .HasMaxLength(2048);

        builder.Property(entity => entity.Notes)
            .HasMaxLength(4000);

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

        builder.HasIndex(entity => entity.Name)
            .IsUnique();
    }
}
