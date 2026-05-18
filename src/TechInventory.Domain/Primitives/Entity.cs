namespace TechInventory.Domain.Primitives;

public abstract class Entity(Guid id)
{
    public Guid Id { get; } = Guard.AgainstDefault(id, nameof(id));

    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    public string? CreatedBy { get; protected set; }

    public DateTimeOffset ModifiedAt { get; protected set; } = DateTimeOffset.UtcNow;

    public string? ModifiedBy { get; protected set; }

    public void SetAuditMetadata(DateTimeOffset createdAt, DateTimeOffset modifiedAt, string? createdBy = null, string? modifiedBy = null)
    {
        if (modifiedAt < createdAt)
        {
            throw new ArgumentOutOfRangeException(nameof(modifiedAt), "ModifiedAt cannot be earlier than CreatedAt.");
        }

        CreatedAt = createdAt;
        ModifiedAt = modifiedAt;
        CreatedBy = Guard.AgainstMaxLength(createdBy, nameof(createdBy), 256);
        ModifiedBy = Guard.AgainstMaxLength(modifiedBy, nameof(modifiedBy), 256);
    }

    protected void Touch(string? modifiedBy = null)
    {
        ModifiedAt = DateTimeOffset.UtcNow;
        ModifiedBy = Guard.AgainstMaxLength(modifiedBy, nameof(modifiedBy), 256);
    }
}
