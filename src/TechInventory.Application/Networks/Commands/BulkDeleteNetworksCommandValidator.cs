using TechInventory.Application.BulkOperations;

namespace TechInventory.Application.Networks.Commands;

public sealed class BulkDeleteNetworksCommandValidator : BulkDeleteReferenceEntitiesCommandValidator<BulkDeleteNetworksCommand>
{
    public BulkDeleteNetworksCommandValidator() : base("network")
    {
    }
}
