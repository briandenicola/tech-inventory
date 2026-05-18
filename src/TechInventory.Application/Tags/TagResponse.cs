using TechInventory.Domain.Entities;

namespace TechInventory.Application.Tags;

public sealed record TagResponse(
    Guid Id,
    string Name,
    string? Color,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset ModifiedAt,
    string? ModifiedBy)
{
    public static TagResponse FromEntity(Tag entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new TagResponse(
            entity.Id,
            entity.Name,
            entity.Color,
            entity.IsActive,
            entity.CreatedAt,
            entity.CreatedBy,
            entity.ModifiedAt,
            entity.ModifiedBy);
    }
}
