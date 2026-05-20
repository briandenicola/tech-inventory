namespace TechInventory.Application.Merges;

public interface IMergeReferenceEntityCommand
{
    Guid SourceId { get; }

    Guid TargetId { get; }
}
