namespace TechInventory.IntegrationTests.Support;

/// <summary>
/// A test factory variant that stamps every request with the Member role so
/// integration tests can assert role-based authorization
/// (e.g. /api/v1/audit-events requires Admin and must 403 for Member).
/// </summary>
public sealed class MemberRoleIntegrationTestFactory<TMarker> : IntegrationTestFactory<TMarker>
    where TMarker : class
{
    protected override string TestAuthRole => "Member";
}
