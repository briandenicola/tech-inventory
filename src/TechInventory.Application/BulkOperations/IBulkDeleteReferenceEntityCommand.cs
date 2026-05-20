namespace TechInventory.Application.BulkOperations;

public interface IBulkDeleteReferenceEntityCommand
{
    IReadOnlyList<Guid> Ids { get; }
}
