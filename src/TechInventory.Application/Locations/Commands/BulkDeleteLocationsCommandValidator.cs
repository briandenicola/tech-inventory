using TechInventory.Application.BulkOperations;

namespace TechInventory.Application.Locations.Commands;

public sealed class BulkDeleteLocationsCommandValidator : BulkDeleteReferenceEntitiesCommandValidator<BulkDeleteLocationsCommand>
{
    public BulkDeleteLocationsCommandValidator() : base("location")
    {
    }
}
