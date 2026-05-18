using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.Infrastructure.Persistence;

namespace TechInventory.IntegrationTests.Helpers;

internal static class AuditEventAssertionHelper
{
    public static async Task<AuditEvent> AssertExistsAsync(
        AppDbContext dbContext,
        string entityType,
        string entityId,
        AuditAction action,
        string? actor = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.AuditEvents
            .Where(auditEvent => auditEvent.EntityType == entityType)
            .Where(auditEvent => auditEvent.EntityId == entityId)
            .Where(auditEvent => auditEvent.Action == action);

        if (!string.IsNullOrWhiteSpace(actor))
        {
            query = query.Where(auditEvent => auditEvent.Actor == actor);
        }

        var auditEvent = (await query.ToListAsync(cancellationToken))
            .OrderByDescending(candidate => candidate.Timestamp)
            .FirstOrDefault();

        auditEvent.Should().NotBeNull();
        auditEvent!.AfterPayload.Should().NotBeNullOrWhiteSpace();
        auditEvent.Timestamp.Offset.Should().Be(TimeSpan.Zero);

        return auditEvent;
    }
}
