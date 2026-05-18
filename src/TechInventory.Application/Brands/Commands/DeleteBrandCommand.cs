using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Brands.Commands;

public sealed record DeleteBrandCommand(Guid Id) : IRequest<Result>, IAuditable;

public sealed class DeleteBrandCommandHandler(
    IBrandRepository brandRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<DeleteBrandCommand, Result>
{
    public async Task<Result> Handle(DeleteBrandCommand request, CancellationToken cancellationToken)
    {
        var entityResult = await brandRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entityResult.IsFailure)
        {
            return Result.Failure(entityResult.Error!);
        }

        var entity = entityResult.Value!;
        if (!entity.IsActive)
        {
            return Result.Failure(Error.Conflict($"Brand '{request.Id}' is already inactive."));
        }

        var beforeSnapshot = BrandResponse.FromEntity(entity);
        entity.Deactivate();

        var updateResult = await brandRepository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure)
        {
            return Result.Failure(updateResult.Error!);
        }

        auditContext.Set(new AuditContextEntry(nameof(Brand), entity.Id.ToString(), AuditAction.Deleted, beforePayload: beforeSnapshot));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
