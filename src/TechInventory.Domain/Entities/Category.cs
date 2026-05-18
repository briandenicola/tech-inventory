using TechInventory.Domain.Primitives;

namespace TechInventory.Domain.Entities;

public sealed class Category(Guid id, string name, Guid? parentId = null, int depth = 1, string? icon = null) : AggregateRoot(id)
{
    public string Name { get; private set; } = Guard.AgainstNullOrWhiteSpace(name, nameof(name), 200);

    public Guid? ParentId { get; private set; } = ValidateParentId(parentId, depth);

    public int Depth { get; private set; } = ValidateDepth(parentId, depth);

    public string? Icon { get; private set; } = Guard.AgainstMaxLength(icon, nameof(icon), 100);

    public bool IsActive { get; private set; } = true;

    public string NormalizedName => Name.ToUpperInvariant();

    public void Rename(string name, string? modifiedBy = null)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name), 200);
        Touch(modifiedBy);
    }

    public void Reparent(Guid? parentId, int depth, string? modifiedBy = null)
    {
        ParentId = ValidateParentId(parentId, depth);
        Depth = ValidateDepth(parentId, depth);
        Touch(modifiedBy);
    }

    public void UpdateIcon(string? icon, string? modifiedBy = null)
    {
        Icon = Guard.AgainstMaxLength(icon, nameof(icon), 100);
        Touch(modifiedBy);
    }

    public void Deactivate(string? modifiedBy = null)
    {
        IsActive = false;
        Touch(modifiedBy);
    }

    public void Reactivate(string? modifiedBy = null)
    {
        IsActive = true;
        Touch(modifiedBy);
    }

    private static Guid? ValidateParentId(Guid? parentId, int depth)
    {
        var normalizedParentId = Guard.AgainstOptionalDefault(parentId, nameof(parentId));
        if (normalizedParentId is null && depth != 1)
        {
            throw new ArgumentException("Root categories must have a depth of 1.", nameof(depth));
        }

        if (normalizedParentId is not null && depth == 1)
        {
            throw new ArgumentException("Child categories must have a depth greater than 1.", nameof(depth));
        }

        return normalizedParentId;
    }

    private static int ValidateDepth(Guid? parentId, int depth)
    {
        if (depth < 1 || depth > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(depth), "Category depth must be between 1 and 3.");
        }

        if (parentId is not null && depth < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(depth), "Child categories must have a depth of 2 or 3.");
        }

        return depth;
    }
}
