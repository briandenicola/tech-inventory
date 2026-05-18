using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechInventory.Domain.Entities;
using TechInventory.Domain.ValueObjects;

namespace TechInventory.Infrastructure.Persistence.Configurations;

public sealed class HouseholdConfiguration : IEntityTypeConfiguration<Household>
{
    public void Configure(EntityTypeBuilder<Household> builder)
    {
        builder.ToTable("Households");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        builder.Property(entity => entity.Name)
            .HasMaxLength(200)
            .UseCollation("NOCASE")
            .IsRequired();

        builder.Property(entity => entity.DefaultCurrency)
            .HasConversion(currency => currency.Code, code => Currency.From(code))
            .HasMaxLength(3)
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
