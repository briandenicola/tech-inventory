using TechInventory.Domain.Primitives;
using TechInventory.Domain.ValueObjects;

namespace TechInventory.Domain.Entities;

public sealed class Household(Guid id, string name, Currency defaultCurrency) : AggregateRoot(id)
{
    public string Name { get; private set; } = Guard.AgainstNullOrWhiteSpace(name, nameof(name), 200);

    public Currency DefaultCurrency { get; private set; } = defaultCurrency ?? throw new ArgumentNullException(nameof(defaultCurrency));

    public void Rename(string name, string? modifiedBy = null)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name), 200);
        Touch(modifiedBy);
    }

    public void SetDefaultCurrency(Currency defaultCurrency, string? modifiedBy = null)
    {
        DefaultCurrency = defaultCurrency ?? throw new ArgumentNullException(nameof(defaultCurrency));
        Touch(modifiedBy);
    }
}
