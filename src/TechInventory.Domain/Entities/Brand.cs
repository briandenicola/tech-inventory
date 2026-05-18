using TechInventory.Domain.Primitives;

namespace TechInventory.Domain.Entities;

public sealed class Brand(Guid id, string name, string? website = null, string? notes = null) : AggregateRoot(id)
{
    public string Name { get; private set; } = Guard.AgainstNullOrWhiteSpace(name, nameof(name), 200);

    public string? Website { get; private set; } = ValidateWebsite(website);

    public string? Notes { get; private set; } = Guard.AgainstMaxLength(notes, nameof(notes), 4000);

    public bool IsActive { get; private set; } = true;

    public string NormalizedName => Name.ToUpperInvariant();

    public void Rename(string name, string? modifiedBy = null)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name), 200);
        Touch(modifiedBy);
    }

    public void UpdateDetails(string? website, string? notes, string? modifiedBy = null)
    {
        Website = ValidateWebsite(website);
        Notes = Guard.AgainstMaxLength(notes, nameof(notes), 4000);
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

    private static string? ValidateWebsite(string? website)
    {
        var normalized = Guard.AgainstMaxLength(website, nameof(website), 2048);
        if (normalized is null)
        {
            return null;
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out _))
        {
            throw new ArgumentException("website must be an absolute URI.", nameof(website));
        }

        return normalized;
    }
}
