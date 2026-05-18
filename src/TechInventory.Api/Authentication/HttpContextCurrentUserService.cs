using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TechInventory.Application.Abstractions.Services;

namespace TechInventory.Api.Authentication;

public sealed class HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string GetCurrentUserId()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return "system";
        }

        return user.FindFirst("oid")?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.Identity?.Name
            ?? "system";
    }
}
