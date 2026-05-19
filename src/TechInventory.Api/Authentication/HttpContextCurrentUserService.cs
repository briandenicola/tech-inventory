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

        return user.FindFirst("oid")?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.Identity?.Name
            ?? "system";
    }

    public string? GetDisplayName()
    {
        var user = GetAuthenticatedUser();
        return user?.FindFirst(ClaimTypes.Name)?.Value
            ?? user?.Identity?.Name;
    }

    public string? GetRoleClaim()
    {
        var user = GetAuthenticatedUser();
        return user?.FindFirst(ClaimTypes.Role)?.Value;
    }

    private ClaimsPrincipal? GetAuthenticatedUser()
    {
        var user = httpContextAccessor.HttpContext?.User;
        return user?.Identity?.IsAuthenticated == true ? user : null;
    }
}
