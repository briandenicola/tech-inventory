using TechInventory.Domain.Entities;

namespace TechInventory.Application.Networks;

public sealed record NetworkResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset ModifiedAt,
    string? ModifiedBy)
{
    public static NetworkResponse FromEntity(Network entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new NetworkResponse(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.IsActive,
            entity.CreatedAt,
            entity.CreatedBy,
            entity.ModifiedAt,
            entity.ModifiedBy);
    }
}
