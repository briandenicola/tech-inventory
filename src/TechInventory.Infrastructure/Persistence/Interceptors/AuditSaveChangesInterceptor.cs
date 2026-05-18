using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TechInventory.Application.Abstractions.Services;
using TechInventory.Domain.Primitives;

namespace TechInventory.Infrastructure.Persistence.Interceptors;

public sealed class AuditSaveChangesInterceptor(ICurrentUserService currentUserService) : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        StampAuditColumns(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StampAuditColumns(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void StampAuditColumns(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var actor = _currentUserService.GetCurrentUserId();
        var utcNow = DateTimeOffset.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetAuditMetadata(utcNow, utcNow, actor, actor);
                    break;
                case EntityState.Modified:
                    entry.Property(nameof(Entity.CreatedAt)).IsModified = false;
                    entry.Property(nameof(Entity.CreatedBy)).IsModified = false;
                    entry.Entity.SetAuditMetadata(entry.Entity.CreatedAt, utcNow, entry.Entity.CreatedBy, actor);
                    break;
            }
        }
    }
}
