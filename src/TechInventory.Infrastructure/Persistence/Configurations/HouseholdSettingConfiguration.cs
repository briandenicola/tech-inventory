using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Configurations;

public sealed class HouseholdSettingConfiguration : IEntityTypeConfiguration<HouseholdSetting>
{
    public void Configure(EntityTypeBuilder<HouseholdSetting> builder)
    {
        builder.ToTable("HouseholdSettings");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        builder.Property(entity => entity.HouseholdId)
            .IsRequired();

        builder.Property(entity => entity.Key)
            .HasMaxLength(100)
            .UseCollation("NOCASE")
            .IsRequired();

        builder.Property(entity => entity.Value)
            .HasMaxLength(8000)
            .IsRequired();

        builder.Property(entity => entity.CreatedAt)
            .IsRequired();

        builder.Property(entity => entity.CreatedBy)
            .HasMaxLength(256);

        builder.Property(entity => entity.ModifiedAt)
            .IsRequired();

        builder.Property(entity => entity.ModifiedBy)
            .HasMaxLength(256);

        builder.HasOne<Household>()
            .WithMany()
            .HasForeignKey(entity => entity.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => new { entity.HouseholdId, entity.Key })
            .IsUnique();
    }
}
