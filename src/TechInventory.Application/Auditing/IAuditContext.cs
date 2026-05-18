namespace TechInventory.Application.Auditing;

public interface IAuditContext
{
    AuditContextEntry? Current { get; }

    void Set(AuditContextEntry entry);

    void Clear();
}
