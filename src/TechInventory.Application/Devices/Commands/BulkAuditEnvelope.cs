namespace TechInventory.Application.BulkOperations;

/// <summary>
/// Wrapper persisted into AuditEvent.BeforePayload / AfterPayload by bulk
/// command handlers so every audit row from a single bulk operation can be
/// correlated without altering the AuditEvent schema. Single-device handlers
/// continue to write the bare snapshot directly.
/// </summary>
public sealed record BulkAuditEnvelope(Guid CorrelationId, object Payload);
