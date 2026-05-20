using TechInventory.Application.BulkOperations;

namespace TechInventory.Application.Categories.Commands;

public sealed class BulkDeleteCategoriesCommandValidator : BulkDeleteReferenceEntitiesCommandValidator<BulkDeleteCategoriesCommand>
{
    public BulkDeleteCategoriesCommandValidator() : base("category")
    {
    }
}
