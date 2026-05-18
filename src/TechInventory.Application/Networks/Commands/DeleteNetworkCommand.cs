using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Networks.Commands;

public sealed record DeleteNetworkCommand(Guid Id) : IRequest<Result>, IAuditable;

public sealed class DeleteNetworkCommandHandler(
    INetworkRepository networkRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<DeleteNetworkCommand, Result>
{
    public async Task<Result> Handle(DeleteNetworkCommand request, CancellationToken cancellationToken)
    {
        var entityResult = await networkRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entityResult.IsFailure)
        {
            return Result.Failure(entityResult.Error!);
        }

        var entity = entityResult.Value!;
        if (!entity.IsActive)
        {
            return Result.Failure(Error.Conflict($"Network '{request.Id}' is already inactive."));
        }

        var beforeSnapshot = NetworkResponse.FromEntity(entity);
        entity.Deactivate();

        var updateResult = await networkRepository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure)
        {
            return Result.Failure(updateResult.Error!);
        }

        auditContext.Set(new AuditContextEntry(nameof(Network), entity.Id.ToString(), AuditAction.Deleted, beforePayload: beforeSnapshot));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
