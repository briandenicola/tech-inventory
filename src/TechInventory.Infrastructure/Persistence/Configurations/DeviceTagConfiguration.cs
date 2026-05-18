using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Configurations;

public sealed class DeviceTagConfiguration : IEntityTypeConfiguration<DeviceTag>
{
    public void Configure(EntityTypeBuilder<DeviceTag> builder)
    {
        builder.ToTable("DeviceTags");
        builder.HasKey(entity => new { entity.DeviceId, entity.TagId });

        builder.Property(entity => entity.IsActive)
            .IsRequired();

        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(entity => entity.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tag>()
            .WithMany()
            .HasForeignKey(entity => entity.TagId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
