namespace TechInventory.Domain.Enums;

/// <summary>
/// Coarse, inventory-only capability carried by an API key (ADR 0003).
/// Deliberately not a general permission model: keys never reach admin,
/// audit, import, export, report, settings, or local-auth endpoints
/// regardless of the live role of the principal that issued them.
/// </summary>
public enum ApiKeyScope
{
    /// <summary>GET on devices plus reference data (brand, category, location, network, owner, tag).</summary>
    Read = 1,

    /// <summary>Everything <see cref="Read"/> grants, plus POST/PUT/DELETE on devices.</summary>
    Write = 2,
}
