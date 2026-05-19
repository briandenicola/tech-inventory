using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TechInventory.Application.Abstractions.Services;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Services;

/// <summary>
/// F025 — issues HS256-signed JWTs for locally-authenticated users.
///
/// Claim shape mirrors the Entra JWT so existing <c>[Authorize(Roles=...)]</c>
/// attributes and <see cref="HttpContextCurrentUserService"/> see no difference:
/// <c>sub</c> + <c>oid</c> = LocalUser.Id, <c>name</c> = DisplayName,
/// <c>ClaimTypes.Role</c> = role string, and a custom <c>auth_method = local</c>
/// + <c>must_change_password</c> so callers can render the right UX.
/// </summary>
public sealed class HmacJwtLocalTokenIssuer(IOptions<LocalJwtOptions> options, TimeProvider timeProvider) : ILocalTokenIssuer
{
    private readonly LocalJwtOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(_options.AccessTokenLifetimeMinutes);

    public (string Token, DateTimeOffset ExpiresAtUtc) IssueAccessToken(LocalUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(_options.SigningKey) || Encoding.UTF8.GetByteCount(_options.SigningKey) < 32)
        {
            throw new InvalidOperationException("Auth:Local:SigningKey is required and must be at least 32 bytes (256 bits) when local auth is enabled.");
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var expiresAtUtc = nowUtc.Add(AccessTokenLifetime);

        // Emit the short JSON claim names ("role", "name") that downstream
        // JWT consumers (our SvelteKit frontend + standard JwtBearer inbound
        // claim-type mapping) expect. Constructing JwtSecurityToken directly
        // skips ClaimTypeMapping.DefaultOutboundClaimTypeMap, so if we left
        // ClaimTypes.Role / ClaimTypes.Name in here the JWT JSON would carry
        // the long XML URIs as keys and the frontend would silently default
        // to Viewer / "Local user" (which is exactly what happened on first
        // F025 prod sign-in, 2026-05-19).
        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new("oid", user.Id.ToString()),
            new("name", user.DisplayName),
            new("preferred_username", user.Username),
            new("role", user.Role.ToString()),
            new("auth_method", "local"),
            new("must_change_password", user.MustChangePasswordOnNextLogin ? "true" : "false")
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: LocalJwtOptions.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: nowUtc.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return (token, expiresAtUtc);
    }
}

/// <summary>
/// Bound to <c>Auth:Local:*</c>. Marker the <c>iss</c> claim uses so the API
/// can route inbound tokens to the right validator.
/// </summary>
public sealed class LocalJwtOptions
{
    public const string SectionPath = "Auth:Local";
    public const string Issuer = "techinventory-local";

    public string? SigningKey { get; set; }

    public string Audience { get; set; } = "techinventory-api";

    public int AccessTokenLifetimeMinutes { get; set; } = 8 * 60;

    public bool Enabled { get; set; }

    public bool SeedEnabled { get; set; }

    public string? SeedUsername { get; set; }

    public string? SeedPassword { get; set; }

    public bool SeedAllowInProd { get; set; }

    /// <summary>
    /// When true (default), the seeded local admin is forced through the
    /// change-password flow on first login. The break-glass account is
    /// intended to be rotated to a real operator-chosen password
    /// immediately, so this default is safe. E2E and local-dev workflows
    /// flip this to false in their config so the seeded admin can sign in
    /// directly with the configured password.
    /// </summary>
    public bool SeedRequireChangeOnFirstLogin { get; set; } = true;
}
