using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Tags.Commands;

public sealed record UpdateTagCommand(Guid Id, string Name, string? Color = null) : IRequest<Result<TagResponse>>, IAuditable;

public sealed class UpdateTagCommandHandler(
    ITagRepository tagRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<UpdateTagCommand, Result<TagResponse>>
{
    public async Task<Result<TagResponse>> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
    {
        var entityResult = await tagRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entityResult.IsFailure)
        {
            return Result<TagResponse>.Failure(entityResult.Error!);
        }

        var entity = entityResult.Value!;
        var duplicateResult = await tagRepository.GetByNormalizedNameAsync(request.Name, cancellationToken).ConfigureAwait(false);
        if (duplicateResult.IsSuccess && duplicateResult.Value!.Id != entity.Id)
        {
            return Result<TagResponse>.Failure(Error.Conflict($"Tag with name '{request.Name.Trim()}' already exists."));
        }

        var beforeSnapshot = TagResponse.FromEntity(entity);

        try
        {
            entity.Rename(request.Name);
            entity.UpdateColor(request.Color);
            if (!entity.IsActive)
            {
                entity.Reactivate();
            }
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            return Result<TagResponse>.Failure(Error.Conflict(exception.Message));
        }

        var updateResult = await tagRepository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure)
        {
            return Result<TagResponse>.Failure(updateResult.Error!);
        }

        auditContext.Set(new AuditContextEntry(nameof(Tag), entity.Id.ToString(), AuditAction.Updated, beforePayload: beforeSnapshot));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<TagResponse>.Success(TagResponse.FromEntity(entity));
    }
}
