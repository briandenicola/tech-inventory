using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(entity => entity.Id);
        builder.Ignore(entity => entity.NormalizedName);

        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        builder.Property(entity => entity.Name)
            .HasMaxLength(200)
            .UseCollation("NOCASE")
            .IsRequired();

        builder.Property(entity => entity.Icon)
            .HasMaxLength(100);

        builder.Property(entity => entity.Depth)
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

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(entity => entity.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.ParentId, entity.Name });
    }
}
