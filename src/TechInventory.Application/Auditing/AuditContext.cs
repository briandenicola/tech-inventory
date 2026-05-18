namespace TechInventory.Application.Auditing;

public sealed class AuditContext : IAuditContext
{
    public AuditContextEntry? Current { get; private set; }

    public void Set(AuditContextEntry entry)
    {
        Current = entry ?? throw new ArgumentNullException(nameof(entry));
    }

    public void Clear()
    {
        Current = null;
    }
}
