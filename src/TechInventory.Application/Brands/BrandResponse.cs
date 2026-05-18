using TechInventory.Domain.Entities;

namespace TechInventory.Application.Brands;

public sealed record BrandResponse(
    Guid Id,
    string Name,
    string? Website,
    string? Notes,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset ModifiedAt,
    string? ModifiedBy)
{
    public static BrandResponse FromEntity(Brand entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new BrandResponse(
            entity.Id,
            entity.Name,
            entity.Website,
            entity.Notes,
            entity.IsActive,
            entity.CreatedAt,
            entity.CreatedBy,
            entity.ModifiedAt,
            entity.ModifiedBy);
    }
}
