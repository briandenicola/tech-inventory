namespace TechInventory.Application.Auth.Commands;

/// <summary>
/// F025 — payload returned to clients after a successful local sign-in.
/// </summary>
/// <param name="AccessToken">Signed JWT to be sent as <c>Authorization: Bearer ...</c>.</param>
/// <param name="ExpiresInSeconds">Lifetime hint (seconds remaining), suitable for UI refresh timers.</param>
/// <param name="MustChangePassword">
/// True when the caller must immediately POST to
/// <c>/api/v1/auth/local/change-password</c> before any other endpoint will
/// honour their token. The token itself carries the same flag in the
/// <c>must_change_password</c> claim so the API enforces it server-side.
/// </param>
public sealed record LocalLoginResponse(
    string AccessToken,
    long ExpiresInSeconds,
    bool MustChangePassword);
