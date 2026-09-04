namespace TechInventory.Application.Abstractions.Services;

public interface ICurrentUserService
{
    string GetCurrentUserId();

    string? GetDisplayName();

    string? GetRoleClaim();

    /// <summary>
    /// The <c>auth_method</c> claim, or <see langword="null"/> when the caller did not
    /// arrive with one.
    /// </summary>
    /// <remarks>
    /// #149: an Entra <c>Owner</c> and an F025 break-glass <c>LocalUser</c> both present a
    /// GUID as their subject, so the two cannot be told apart by the id alone. The local
    /// token issuer stamps <c>auth_method = local</c>; its absence means Entra. This is the
    /// only signal available to decide which repository owns a caller's identity.
    /// </remarks>
    string? GetAuthenticationMethod();
}
