using TechInventory.Domain.Entities;

namespace TechInventory.Application.Categories;

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    Guid? ParentId,
    int Depth,
    string? Icon,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset ModifiedAt,
    string? ModifiedBy,
    IReadOnlyList<CategoryResponse> Children)
{
    public static CategoryResponse FromEntity(Category category, IReadOnlyList<CategoryResponse>? children = null)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new CategoryResponse(
            category.Id,
            category.Name,
            category.ParentId,
            category.Depth,
            category.Icon,
            category.IsActive,
            category.CreatedAt,
            category.CreatedBy,
            category.ModifiedAt,
            category.ModifiedBy,
            children ?? []);
    }
}
