using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Brands.Commands;

public sealed record UpdateBrandCommand(Guid Id, string Name, string? Website = null, string? Notes = null) : IRequest<Result<BrandResponse>>, IAuditable;

public sealed class UpdateBrandCommandHandler(
    IBrandRepository brandRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<UpdateBrandCommand, Result<BrandResponse>>
{
    public async Task<Result<BrandResponse>> Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
    {
        var entityResult = await brandRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entityResult.IsFailure)
        {
            return Result<BrandResponse>.Failure(entityResult.Error!);
        }

        var entity = entityResult.Value!;
        var duplicateResult = await brandRepository.GetByNormalizedNameAsync(request.Name, cancellationToken).ConfigureAwait(false);
        if (duplicateResult.IsSuccess && duplicateResult.Value!.Id != entity.Id)
        {
            return Result<BrandResponse>.Failure(Error.Conflict($"Brand with name '{request.Name.Trim()}' already exists."));
        }

        var beforeSnapshot = BrandResponse.FromEntity(entity);

        try
        {
            entity.Rename(request.Name);
            entity.UpdateDetails(request.Website, request.Notes);
            if (!entity.IsActive)
            {
                entity.Reactivate();
            }
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            return Result<BrandResponse>.Failure(Error.Conflict(exception.Message));
        }

        var updateResult = await brandRepository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure)
        {
            return Result<BrandResponse>.Failure(updateResult.Error!);
        }

        auditContext.Set(new AuditContextEntry(nameof(Brand), entity.Id.ToString(), AuditAction.Updated, beforePayload: beforeSnapshot));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<BrandResponse>.Success(BrandResponse.FromEntity(entity));
    }
}
