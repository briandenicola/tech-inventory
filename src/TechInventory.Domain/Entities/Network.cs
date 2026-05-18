using TechInventory.Domain.Primitives;

namespace TechInventory.Domain.Entities;

public sealed class Network(Guid id, string name, string? description = null) : AggregateRoot(id)
{
    public string Name { get; private set; } = Guard.AgainstNullOrWhiteSpace(name, nameof(name), 200);

    public string? Description { get; private set; } = Guard.AgainstMaxLength(description, nameof(description), 1000);

    public bool IsActive { get; private set; } = true;

    public string NormalizedName => Name.ToUpperInvariant();

    public void Rename(string name, string? modifiedBy = null)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name), 200);
        Touch(modifiedBy);
    }

    public void UpdateDescription(string? description, string? modifiedBy = null)
    {
        Description = Guard.AgainstMaxLength(description, nameof(description), 1000);
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
