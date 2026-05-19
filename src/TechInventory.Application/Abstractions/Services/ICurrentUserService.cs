namespace TechInventory.Application.Abstractions.Services;

public interface ICurrentUserService
{
    string GetCurrentUserId();

    string? GetDisplayName();

    string? GetRoleClaim();
}
