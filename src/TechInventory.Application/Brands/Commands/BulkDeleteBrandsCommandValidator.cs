using TechInventory.Application.BulkOperations;

namespace TechInventory.Application.Brands.Commands;

public sealed class BulkDeleteBrandsCommandValidator : BulkDeleteReferenceEntitiesCommandValidator<BulkDeleteBrandsCommand>
{
    public BulkDeleteBrandsCommandValidator() : base("brand")
    {
    }
}
