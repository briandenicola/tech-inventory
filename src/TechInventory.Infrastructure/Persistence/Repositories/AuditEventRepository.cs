using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Repositories;

public sealed class AuditEventRepository(AppDbContext dbContext) : IAuditEventRepository
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<Result<AuditEvent>> AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        await _dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken).ConfigureAwait(false);
        return Result<AuditEvent>.Success(auditEvent);
    }

    public async Task<Result<AuditEvent>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var auditEvent = _dbContext.AuditEvents.Local.FirstOrDefault(entity => entity.Id == id)
            ?? await _dbContext.AuditEvents
                .SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken)
                .ConfigureAwait(false);

        return auditEvent is null
            ? Result<AuditEvent>.Failure(new Error("NotFound", $"AuditEvent '{id}' was not found."))
            : Result<AuditEvent>.Success(auditEvent);
    }

    public async Task<PagedResult<AuditEvent>> ListAsync(AuditEventListCriteria criteria, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        IQueryable<AuditEvent> query = _dbContext.AuditEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(criteria.EntityType))
        {
            query = query.Where(entity => entity.EntityType == criteria.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(criteria.EntityId))
        {
            query = query.Where(entity => entity.EntityId == criteria.EntityId);
        }

        if (criteria.Action.HasValue)
        {
            query = query.Where(entity => entity.Action == criteria.Action.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.ActorId))
        {
            query = query.Where(entity => entity.Actor == criteria.ActorId);
        }

        if (criteria.From.HasValue)
        {
            query = query.Where(entity => entity.Timestamp >= criteria.From.Value);
        }

        if (criteria.To.HasValue)
        {
            query = query.Where(entity => entity.Timestamp <= criteria.To.Value);
        }

        var merged = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        var filteredLocal = _dbContext.AuditEvents.Local.Where(entity => MatchesCriteria(entity, criteria));
        var items = merged
            .Concat(filteredLocal)
            .GroupBy(entity => entity.Id)
            .Select(group => group.Last())
            .OrderByDescending(entity => entity.Timestamp)
            .ThenByDescending(entity => entity.Id)
            .ToArray();

        var pagedItems = items
            .Skip((criteria.PageRequest.Page - 1) * criteria.PageRequest.PageSize)
            .Take(criteria.PageRequest.PageSize)
            .ToArray();

        return new PagedResult<AuditEvent>(pagedItems, items.Length, criteria.PageRequest.Page, criteria.PageRequest.PageSize);
    }

    private static bool MatchesCriteria(AuditEvent auditEvent, AuditEventListCriteria criteria)
    {
        return (string.IsNullOrWhiteSpace(criteria.EntityType) || string.Equals(auditEvent.EntityType, criteria.EntityType, StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(criteria.EntityId) || string.Equals(auditEvent.EntityId, criteria.EntityId, StringComparison.Ordinal))
            && (!criteria.Action.HasValue || auditEvent.Action == criteria.Action.Value)
            && (string.IsNullOrWhiteSpace(criteria.ActorId) || string.Equals(auditEvent.Actor, criteria.ActorId, StringComparison.OrdinalIgnoreCase))
            && (!criteria.From.HasValue || auditEvent.Timestamp >= criteria.From.Value)
            && (!criteria.To.HasValue || auditEvent.Timestamp <= criteria.To.Value);
    }
}
