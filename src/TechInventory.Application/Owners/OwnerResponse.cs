using TechInventory.Domain.Entities;

namespace TechInventory.Application.Owners;

public sealed record OwnerResponse(
    Guid Id,
    string DisplayName,
    string Role,
    Guid? EntraObjectId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset ModifiedAt,
    string? ModifiedBy)
{
    public static OwnerResponse FromEntity(Owner owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        return new OwnerResponse(
            owner.Id,
            owner.DisplayName,
            owner.Role.ToString(),
            owner.EntraObjectId,
            owner.IsActive,
            owner.CreatedAt,
            owner.CreatedBy,
            owner.ModifiedAt,
            owner.ModifiedBy);
    }
}
