using TechInventory.Application.Common.Paging;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Abstractions.Repositories;

public sealed record AuditEventListCriteria
{
    public AuditEventListCriteria(
        PageRequest pageRequest,
        string? entityType = null,
        string? entityId = null,
        AuditAction? action = null,
        string? actorId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null)
    {
        if (from.HasValue && to.HasValue && from > to)
        {
            throw new ArgumentOutOfRangeException(nameof(from), "from cannot be later than to.");
        }

        PageRequest = pageRequest ?? throw new ArgumentNullException(nameof(pageRequest));
        EntityType = string.IsNullOrWhiteSpace(entityType) ? null : entityType.Trim();
        EntityId = string.IsNullOrWhiteSpace(entityId) ? null : entityId.Trim();
        Action = action;
        ActorId = string.IsNullOrWhiteSpace(actorId) ? null : actorId.Trim();
        From = from;
        To = to;
    }

    public PageRequest PageRequest { get; }

    public string? EntityType { get; }

    public string? EntityId { get; }

    public AuditAction? Action { get; }

    public string? ActorId { get; }

    public DateTimeOffset? From { get; }

    public DateTimeOffset? To { get; }
}
