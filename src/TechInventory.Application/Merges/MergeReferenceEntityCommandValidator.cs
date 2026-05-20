using FluentValidation;

namespace TechInventory.Application.Merges;

public abstract class MergeReferenceEntityCommandValidator<TCommand> : AbstractValidator<TCommand>
    where TCommand : IMergeReferenceEntityCommand
{
    protected MergeReferenceEntityCommandValidator()
    {
        RuleFor(command => command.SourceId)
            .NotEmpty();

        RuleFor(command => command.TargetId)
            .NotEmpty();

        RuleFor(command => command)
            .Must(command => command.SourceId != command.TargetId)
            .WithMessage("SourceId and TargetId must be different.");
    }
}
