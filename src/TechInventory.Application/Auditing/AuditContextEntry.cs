using TechInventory.Domain.Enums;

namespace TechInventory.Application.Auditing;

public sealed record AuditContextEntry
{
    public AuditContextEntry(
        string entityType,
        string entityId,
        AuditAction action,
        object? beforePayload = null,
        object? afterPayload = null,
        string? actor = null)
    {
        EntityType = string.IsNullOrWhiteSpace(entityType)
            ? throw new ArgumentException("EntityType is required.", nameof(entityType))
            : entityType.Trim();
        EntityId = string.IsNullOrWhiteSpace(entityId)
            ? throw new ArgumentException("EntityId is required.", nameof(entityId))
            : entityId.Trim();
        Action = action;
        BeforePayload = beforePayload;
        AfterPayload = afterPayload;
        Actor = string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
    }

    public string EntityType { get; }

    public string EntityId { get; }

    public AuditAction Action { get; }

    public object? BeforePayload { get; }

    public object? AfterPayload { get; }

    public string? Actor { get; }
}
