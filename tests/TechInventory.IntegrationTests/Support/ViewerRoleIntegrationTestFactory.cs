namespace TechInventory.IntegrationTests.Support;

/// <summary>
/// A test factory variant that stamps every request with the Viewer role so
/// integration tests can assert Viewer-role authorization — e.g. Viewer must
/// receive 403 on Admin-gated actions (merge, bulk-delete, audit events) and
/// on AdminOrMember-gated mutations (device create/update/delete/tag/claim/
/// release/bulk-update, import commit, reference-entity create/update/
/// delete), mirroring <see cref="MemberRoleIntegrationTestFactory{T}"/>.
/// </summary>
public sealed class ViewerRoleIntegrationTestFactory<TMarker> : IntegrationTestFactory<TMarker>
    where TMarker : class
{
    protected override string TestAuthRole => "Viewer";
}
