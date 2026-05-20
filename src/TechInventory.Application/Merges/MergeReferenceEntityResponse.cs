namespace TechInventory.Application.Merges;

public sealed record MergeReferenceEntityResponse(int MergedCount, Guid SourceId, Guid TargetId);
