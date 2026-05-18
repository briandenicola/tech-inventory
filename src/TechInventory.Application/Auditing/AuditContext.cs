namespace TechInventory.Application.Auditing;

public sealed class AuditContext : IAuditContext
{
    private readonly List<AuditContextEntry> _entries = [];

    public AuditContextEntry? Current => _entries.Count == 0 ? null : _entries[^1];

    public IReadOnlyList<AuditContextEntry> Entries => _entries;

    public void Set(AuditContextEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _entries.Clear();
        _entries.Add(entry);
    }

    public void Add(AuditContextEntry entry)
    {
        _entries.Add(entry ?? throw new ArgumentNullException(nameof(entry)));
    }

    public void Clear()
    {
        _entries.Clear();
    }
}
