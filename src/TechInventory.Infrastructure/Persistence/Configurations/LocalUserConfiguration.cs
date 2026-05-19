using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Configurations;

public sealed class LocalUserConfiguration : IEntityTypeConfiguration<LocalUser>
{
    public void Configure(EntityTypeBuilder<LocalUser> builder)
    {
        builder.ToTable("LocalUsers");
        builder.HasKey(entity => entity.Id);
        builder.Ignore(entity => entity.NormalizedUsername);

        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        builder.Property(entity => entity.Username)
            .HasMaxLength(LocalUser.MaxUsernameLength)
            .UseCollation("NOCASE")
            .IsRequired();

        builder.Property(entity => entity.DisplayName)
            .HasMaxLength(LocalUser.MaxDisplayNameLength)
            .IsRequired();

        builder.Property(entity => entity.Role)
            .IsRequired();

        builder.Property(entity => entity.PasswordHash)
            .HasMaxLength(LocalUser.MaxHashLength)
            .IsRequired();

        builder.Property(entity => entity.PasswordAlgorithm)
            .HasMaxLength(LocalUser.MaxAlgorithmLength)
            .IsRequired();

        builder.Property(entity => entity.MustChangePasswordOnNextLogin)
            .IsRequired();

        builder.Property(entity => entity.FailedAttemptCount)
            .IsRequired();

        builder.Property(entity => entity.LockoutUntilUtc);
        builder.Property(entity => entity.LastLoginUtc);

        builder.Property(entity => entity.LastPasswordChangeUtc)
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .IsRequired();

        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.CreatedBy).HasMaxLength(256);
        builder.Property(entity => entity.ModifiedAt).IsRequired();
        builder.Property(entity => entity.ModifiedBy).HasMaxLength(256);

        builder.HasIndex(entity => entity.Username)
            .IsUnique();
    }
}
