using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Configurations;

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.HasKey(entity => entity.Id);
        builder.Ignore(entity => entity.NormalizedName);

        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        builder.Property(entity => entity.Name)
            .HasMaxLength(200)
            .UseCollation("NOCASE")
            .IsRequired();

        builder.Property(entity => entity.Color)
            .HasMaxLength(32);

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
