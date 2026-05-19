using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TechInventory.Application.Abstractions.Services;

namespace TechInventory.Api.Authentication;

public sealed class HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string GetCurrentUserId()
    {
        var user = GetAuthenticatedUser();
        if (user is null)
        {
            return "system";
        }

        // Entra JwtBearer runs with MapInboundClaims = true by default, which
        // rewrites short JWT claim names ("oid", "sub") into the SOAP-era URIs
        // below before the ClaimsPrincipal is built. Check BOTH so we work
        // regardless of inbound-mapping configuration (local HS256 tokens
        // arrive with the short names, Entra v1/v2 tokens with the URIs).
        return user.FindFirst("oid")?.Value
            ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.Identity?.Name
            ?? "system";
    }

    public string? GetDisplayName()
    {
        var user = GetAuthenticatedUser();
        return user?.FindFirst("name")?.Value
            ?? user?.FindFirst(ClaimTypes.Name)?.Value
            ?? user?.Identity?.Name;
    }

    public string? GetRoleClaim()
    {
        var user = GetAuthenticatedUser();
        return user?.FindFirst(ClaimTypes.Role)?.Value
            ?? user?.FindFirst("roles")?.Value
            ?? user?.FindFirst("role")?.Value;
    }

    private ClaimsPrincipal? GetAuthenticatedUser()
    {
        var user = httpContextAccessor.HttpContext?.User;
        return user?.Identity?.IsAuthenticated == true ? user : null;
    }
}
