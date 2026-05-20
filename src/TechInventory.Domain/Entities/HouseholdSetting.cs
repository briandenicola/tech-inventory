using TechInventory.Domain.Primitives;

namespace TechInventory.Domain.Entities;

public sealed class HouseholdSetting(Guid id, Guid householdId, string key, string value) : AggregateRoot(id)
{
    public Guid HouseholdId { get; private set; } = Guard.AgainstDefault(householdId, nameof(householdId));

    public string Key { get; private set; } = Guard.AgainstNullOrWhiteSpace(key, nameof(key), 100);

    public string Value { get; private set; } = Guard.AgainstNullOrWhiteSpace(value, nameof(value), 8000);

    public void UpdateValue(string value, string? modifiedBy = null)
    {
        Value = Guard.AgainstNullOrWhiteSpace(value, nameof(value), 8000);
        Touch(modifiedBy);
    }
}
