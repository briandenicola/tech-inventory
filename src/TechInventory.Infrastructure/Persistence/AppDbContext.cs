using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Domain.Entities;
using TechInventory.Infrastructure.Persistence.Interceptors;

namespace TechInventory.Infrastructure.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    AuditSaveChangesInterceptor auditSaveChangesInterceptor) : DbContext(options), IUnitOfWork
{
    private readonly AuditSaveChangesInterceptor _auditSaveChangesInterceptor = auditSaveChangesInterceptor ?? throw new ArgumentNullException(nameof(auditSaveChangesInterceptor));

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Device> Devices => Set<Device>();

    public DbSet<DeviceTag> DeviceTags => Set<DeviceTag>();

    public DbSet<Household> Households => Set<Household>();

    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<Network> Networks => Set<Network>();

    public DbSet<Owner> Owners => Set<Owner>();

    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.AddInterceptors(_auditSaveChangesInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceImmutableRecords();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnforceImmutableRecords();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void EnforceImmutableRecords()
    {
        ThrowIfModifiedOrDeleted<AuditEvent>("AuditEvent rows are append-only and cannot be updated or deleted.");
        ThrowIfModifiedOrDeleted<ImportBatch>("ImportBatch rows are immutable after creation and cannot be updated or deleted.");
    }

    private void ThrowIfModifiedOrDeleted<TEntity>(string message)
        where TEntity : class
    {
        var hasForbiddenEntries = ChangeTracker
            .Entries<TEntity>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted);

        if (hasForbiddenEntries)
        {
            throw new InvalidOperationException(message);
        }
    }
}
