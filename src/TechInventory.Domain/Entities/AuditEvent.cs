using TechInventory.Domain.Enums;
using TechInventory.Domain.Primitives;

namespace TechInventory.Domain.Entities;

public sealed class AuditEvent
{
    private AuditEvent()
    {
        Id = Guid.Empty;
        Actor = null!;
        EntityType = null!;
        EntityId = null!;
        BeforePayload = null!;
        AfterPayload = null!;
    }

    public AuditEvent(
        Guid id,
        string actor,
        string entityType,
        string entityId,
        AuditAction action,
        DateTimeOffset timestamp,
        string beforePayload,
        string afterPayload)
    {
        Id = Guard.AgainstDefault(id, nameof(id));
        Actor = Guard.AgainstNullOrWhiteSpace(actor, nameof(actor), 256);
        EntityType = Guard.AgainstNullOrWhiteSpace(entityType, nameof(entityType), 200);
        EntityId = Guard.AgainstNullOrWhiteSpace(entityId, nameof(entityId), 256);
        Action = action;
        Timestamp = ValidateTimestamp(timestamp);
        BeforePayload = ValidatePayload(beforePayload, nameof(beforePayload));
        AfterPayload = ValidatePayload(afterPayload, nameof(afterPayload));
    }

    public AuditEvent(Guid id, string actor, string entityType, string entityId, AuditAction action, string beforePayload, string afterPayload)
        : this(id, actor, entityType, entityId, action, DateTimeOffset.UtcNow, beforePayload, afterPayload)
    {
    }

    public Guid Id { get; private set; }

    public string Actor { get; private set; }

    public string EntityType { get; private set; }

    public string EntityId { get; private set; }

    public AuditAction Action { get; private set; }

    public DateTimeOffset Timestamp { get; private set; }

    public string BeforePayload { get; private set; }

    public string AfterPayload { get; private set; }

    private static DateTimeOffset ValidateTimestamp(DateTimeOffset timestamp)
    {
        if (timestamp == default)
        {
            throw new ArgumentOutOfRangeException(nameof(timestamp), "timestamp must be provided.");
        }

        return timestamp;
    }

    private static string ValidatePayload(string payload, string paramName)
    {
        return Guard.AgainstNullOrWhiteSpace(payload, paramName, 32_768);
    }
}
