using FluentValidation;

namespace TechInventory.Application.BulkOperations;

public abstract class BulkDeleteReferenceEntitiesCommandValidator<TCommand> : AbstractValidator<TCommand>
    where TCommand : IBulkDeleteReferenceEntityCommand
{
    protected BulkDeleteReferenceEntitiesCommandValidator(string entityLabel)
    {
        RuleFor(command => command.Ids)
            .NotNull()
            .NotEmpty()
            .WithMessage($"At least one {entityLabel} id is required.")
            .Must(ids => ids.Count <= 500)
            .WithMessage("A bulk operation cannot affect more than 500 records in a single request.");

        RuleForEach(command => command.Ids)
            .NotEmpty()
            .WithMessage($"{entityLabel} ids cannot contain empty GUID values.");
    }
}
