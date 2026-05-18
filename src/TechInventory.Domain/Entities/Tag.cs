using TechInventory.Domain.Primitives;

namespace TechInventory.Domain.Entities;

public sealed class Tag(Guid id, string name, string? color = null) : AggregateRoot(id)
{
    public string Name { get; private set; } = Guard.AgainstNullOrWhiteSpace(name, nameof(name), 200);

    public string? Color { get; private set; } = Guard.AgainstMaxLength(color, nameof(color), 32);

    public bool IsActive { get; private set; } = true;

    public string NormalizedName => Name.ToUpperInvariant();

    public void Rename(string name, string? modifiedBy = null)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name), 200);
        Touch(modifiedBy);
    }

    public void UpdateColor(string? color, string? modifiedBy = null)
    {
        Color = Guard.AgainstMaxLength(color, nameof(color), 32);
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
}
