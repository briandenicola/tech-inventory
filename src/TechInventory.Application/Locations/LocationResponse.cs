using TechInventory.Domain.Entities;

namespace TechInventory.Application.Locations;

public sealed record LocationResponse(
    Guid Id,
    string Name,
    string Type,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset ModifiedAt,
    string? ModifiedBy)
{
    public static LocationResponse FromEntity(Location entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new LocationResponse(
            entity.Id,
            entity.Name,
            entity.Type.ToString(),
            entity.IsActive,
            entity.CreatedAt,
            entity.CreatedBy,
            entity.ModifiedAt,
            entity.ModifiedBy);
    }
}
