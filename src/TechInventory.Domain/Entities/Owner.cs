using TechInventory.Domain.Enums;
using TechInventory.Domain.Primitives;

namespace TechInventory.Domain.Entities;

public sealed class Owner(Guid id, string displayName, OwnerRole role = OwnerRole.Member, Guid? entraObjectId = null) : AggregateRoot(id)
{
    public string DisplayName { get; private set; } = Guard.AgainstNullOrWhiteSpace(displayName, nameof(displayName), 200);

    public OwnerRole Role { get; private set; } = role;

    public Guid? EntraObjectId { get; private set; } = Guard.AgainstOptionalDefault(entraObjectId, nameof(entraObjectId));

    public bool IsActive { get; private set; } = true;

    public string NormalizedDisplayName => DisplayName.ToUpperInvariant();

    public void Rename(string displayName, string? modifiedBy = null)
    {
        DisplayName = Guard.AgainstNullOrWhiteSpace(displayName, nameof(displayName), 200);
        Touch(modifiedBy);
    }

    public void SetRole(OwnerRole role, string? modifiedBy = null)
    {
        Role = role;
        Touch(modifiedBy);
    }

    public void LinkEntraIdentity(Guid? entraObjectId, string? modifiedBy = null)
    {
        EntraObjectId = Guard.AgainstOptionalDefault(entraObjectId, nameof(entraObjectId));
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
