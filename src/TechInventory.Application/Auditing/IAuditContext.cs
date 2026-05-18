namespace TechInventory.Application.Auditing;

public interface IAuditContext
{
    AuditContextEntry? Current { get; }

    IReadOnlyList<AuditContextEntry> Entries { get; }

    void Set(AuditContextEntry entry);

    void Add(AuditContextEntry entry);

    void Clear();
}
