using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechInventory.Domain.Entities;
using TechInventory.Domain.ValueObjects;

namespace TechInventory.Infrastructure.Persistence.Configurations;

public sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Devices");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        builder.Property(entity => entity.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(entity => entity.Model)
            .HasMaxLength(200);

        builder.Property(entity => entity.SerialNumber)
            .HasMaxLength(200);

        builder.Property(entity => entity.PurchasePrice)
            .HasPrecision(18, 2);

        builder.Property(entity => entity.Currency)
            .HasConversion(currency => currency.Code, code => Currency.From(code))
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(entity => entity.Notes)
            .HasMaxLength(4000);

        builder.Property(entity => entity.DisposalMethod)
            .HasMaxLength(500);

        builder.Property(entity => entity.Purpose)
            .HasMaxLength(500);

        builder.Property(entity => entity.OperatingSystem)
            .HasMaxLength(100);

        builder.Property(entity => entity.IpAddress)
            .HasMaxLength(45);

        builder.Property(entity => entity.MacAddress)
            .HasMaxLength(17);

        builder.Property(entity => entity.ProductUrl)
            .HasMaxLength(500);

        builder.Property(entity => entity.Version)
            .HasMaxLength(50);

        builder.Property(entity => entity.CreatedAt)
            .IsRequired();

        builder.Property(entity => entity.CreatedBy)
            .HasMaxLength(256);

        builder.Property(entity => entity.ModifiedAt)
            .IsRequired();

        builder.Property(entity => entity.ModifiedBy)
            .HasMaxLength(256);

        builder.HasOne<Brand>()
            .WithMany()
            .HasForeignKey(entity => entity.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(entity => entity.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Owner>()
            .WithMany()
            .HasForeignKey(entity => entity.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(entity => entity.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Network>()
            .WithMany()
            .HasForeignKey(entity => entity.NetworkId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
