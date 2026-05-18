using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Networks.Commands;

public sealed record UpdateNetworkCommand(Guid Id, string Name, string? Description = null) : IRequest<Result<NetworkResponse>>, IAuditable;

public sealed class UpdateNetworkCommandHandler(
    INetworkRepository networkRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<UpdateNetworkCommand, Result<NetworkResponse>>
{
    public async Task<Result<NetworkResponse>> Handle(UpdateNetworkCommand request, CancellationToken cancellationToken)
    {
        var entityResult = await networkRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entityResult.IsFailure)
        {
            return Result<NetworkResponse>.Failure(entityResult.Error!);
        }

        var entity = entityResult.Value!;
        var duplicateResult = await networkRepository.GetByNormalizedNameAsync(request.Name, cancellationToken).ConfigureAwait(false);
        if (duplicateResult.IsSuccess && duplicateResult.Value!.Id != entity.Id)
        {
            return Result<NetworkResponse>.Failure(Error.Conflict($"Network with name '{request.Name.Trim()}' already exists."));
        }

        var beforeSnapshot = NetworkResponse.FromEntity(entity);

        try
        {
            entity.Rename(request.Name);
            entity.UpdateDescription(request.Description);
            if (!entity.IsActive)
            {
                entity.Reactivate();
            }
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            return Result<NetworkResponse>.Failure(Error.Conflict(exception.Message));
        }

        var updateResult = await networkRepository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure)
        {
            return Result<NetworkResponse>.Failure(updateResult.Error!);
        }

        auditContext.Set(new AuditContextEntry(nameof(Network), entity.Id.ToString(), AuditAction.Updated, beforePayload: beforeSnapshot));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<NetworkResponse>.Success(NetworkResponse.FromEntity(entity));
    }
}
