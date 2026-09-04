using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Configurations;

public sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        builder.Property(entity => entity.HouseholdId)
            .IsRequired();

        // NOCASE (same pattern as LocalUser.Username) so the per-principal duplicate-name
        // check is a plain case-insensitive `==` in SQL. Using LIKE instead would treat
        // `_` and `%` inside a user's key name as wildcards.
        builder.Property(entity => entity.Name)
            .HasMaxLength(ApiKey.MaxNameLength)
            .UseCollation("NOCASE")
            .IsRequired();

        builder.Property(entity => entity.Selector)
            .HasMaxLength(ApiKey.MaxSelectorLength)
            .IsRequired();

        builder.Property(entity => entity.VerifierHash)
            .HasMaxLength(ApiKey.MaxVerifierHashLength)
            .IsRequired();

        builder.Property(entity => entity.Scope)
            .IsRequired();

        builder.Property(entity => entity.PrincipalType)
            .IsRequired();

        builder.Property(entity => entity.PrincipalId)
            .IsRequired();

        builder.Property(entity => entity.ExpiresAt)
            .IsRequired();

        builder.Property(entity => entity.RevokedAt);
        builder.Property(entity => entity.RevokedBy).HasMaxLength(256);

        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.CreatedBy).HasMaxLength(256);
        builder.Property(entity => entity.ModifiedAt).IsRequired();
        builder.Property(entity => entity.ModifiedBy).HasMaxLength(256);

        // The authentication hot path is a single lookup by selector, so it must be
        // a unique index — both for speed and because a duplicate selector would make
        // "which key is this?" ambiguous.
        builder.HasIndex(entity => entity.Selector)
            .IsUnique();

        // Listing a principal's own keys.
        builder.HasIndex(entity => new { entity.PrincipalType, entity.PrincipalId });

        // The quota check counts a principal's non-revoked keys on every creation;
        // including RevokedAt lets that count be served from the index alone.
        builder.HasIndex(entity => new { entity.PrincipalType, entity.PrincipalId, entity.RevokedAt });
    }
}
