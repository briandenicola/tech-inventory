using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Tags.Commands;

public sealed record CreateTagCommand(string Name, string? Color = null) : IRequest<Result<TagResponse>>, IAuditable;

public sealed class CreateTagCommandHandler(
    ITagRepository tagRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<CreateTagCommand, Result<TagResponse>>
{
    public async Task<Result<TagResponse>> Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
        var duplicateResult = await tagRepository.GetByNormalizedNameAsync(request.Name, cancellationToken).ConfigureAwait(false);
        if (duplicateResult.IsSuccess)
        {
            return Result<TagResponse>.Failure(Error.Conflict($"Tag with name '{request.Name.Trim()}' already exists."));
        }

        try
        {
            var entity = new Tag(Guid.NewGuid(), request.Name, request.Color);
            var addResult = await tagRepository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            if (addResult.IsFailure)
            {
                return Result<TagResponse>.Failure(addResult.Error!);
            }

            auditContext.Set(new AuditContextEntry(nameof(Tag), entity.Id.ToString(), AuditAction.Created));
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<TagResponse>.Success(TagResponse.FromEntity(entity));
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            return Result<TagResponse>.Failure(Error.Conflict(exception.Message));
        }
    }
}
