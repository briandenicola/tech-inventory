namespace TechInventory.Api.Authentication;

public static class ApiAuthenticationSchemes
{
    public const string DefaultScheme = "TechInventoryAuth";
    public const string EntraScheme = "TechInventoryAuth.Entra";
    public const string LocalScheme = "TechInventoryAuth.Local";

    /// <summary>#149 — opaque API key credentials (ADR 0003).</summary>
    public const string ApiKeyScheme = "TechInventoryAuth.ApiKey";

    /// <summary>#149 — terminal rejection target when both an API key and a bearer token are presented.</summary>
    public const string AmbiguousScheme = "TechInventoryAuth.Ambiguous";
}
