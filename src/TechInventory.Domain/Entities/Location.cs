using TechInventory.Domain.Enums;
using TechInventory.Domain.Primitives;

namespace TechInventory.Domain.Entities;

public sealed class Location(Guid id, string name, LocationType type) : AggregateRoot(id)
{
    public string Name { get; private set; } = Guard.AgainstNullOrWhiteSpace(name, nameof(name), 200);

    public LocationType Type { get; private set; } = type;

    public bool IsActive { get; private set; } = true;

    public string NormalizedName => Name.ToUpperInvariant();

    public void Rename(string name, string? modifiedBy = null)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name), 200);
        Touch(modifiedBy);
    }

    public void SetType(LocationType type, string? modifiedBy = null)
    {
        Type = type;
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
