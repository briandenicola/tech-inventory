using TechInventory.Application.Abstractions.Services;

namespace TechInventory.Infrastructure.Services;

public sealed class SystemCurrentUserService : ICurrentUserService
{
    public string GetCurrentUserId() => "system";

    public string? GetDisplayName() => null;

    public string? GetRoleClaim() => null;
}
