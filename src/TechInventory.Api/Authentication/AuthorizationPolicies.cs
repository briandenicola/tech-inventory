namespace TechInventory.Api.Authentication;

/// <summary>
/// Named authorization policies wired up in <c>Program.cs</c>.
/// Centralizes the literal policy names so controllers and tests can
/// reference them without stringly-typed drift.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Requires an authenticated principal with the <c>Admin</c> role claim.
    /// Used for endpoints that mutate or surface household-wide
    /// administrative data (audit log, future user management, etc.).
    /// </summary>
    public const string Admin = "Admin";
}
