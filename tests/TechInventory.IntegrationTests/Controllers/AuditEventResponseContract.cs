namespace TechInventory.IntegrationTests.Controllers;

internal sealed record AuditEventResponseContract(
    Guid Id,
    string Actor,
    string EntityType,
    string EntityId,
    string Action,
    DateTimeOffset Timestamp,
    string BeforePayload,
    string AfterPayload);
