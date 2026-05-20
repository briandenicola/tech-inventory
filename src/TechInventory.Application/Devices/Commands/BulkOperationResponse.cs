namespace TechInventory.Application.BulkOperations;

/// <summary>
/// Response returned by bulk endpoints (PATCH/DELETE /api/v1/devices/bulk*).
/// CorrelationId matches the value embedded in each emitted AuditEvent so
/// callers can stitch together the affected rows after the fact.
/// </summary>
public sealed record BulkOperationResponse(Guid CorrelationId, int AffectedCount);
