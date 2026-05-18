using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Brands.Commands;

public sealed record CreateBrandCommand(string Name, string? Website = null, string? Notes = null) : IRequest<Result<BrandResponse>>, IAuditable;

public sealed class CreateBrandCommandHandler(
    IBrandRepository brandRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<CreateBrandCommand, Result<BrandResponse>>
{
    public async Task<Result<BrandResponse>> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
    {
        var duplicateResult = await brandRepository.GetByNormalizedNameAsync(request.Name, cancellationToken).ConfigureAwait(false);
        if (duplicateResult.IsSuccess)
        {
            return Result<BrandResponse>.Failure(Error.Conflict($"Brand with name '{request.Name.Trim()}' already exists."));
        }

        try
        {
            var entity = new Brand(Guid.NewGuid(), request.Name, request.Website, request.Notes);
            var addResult = await brandRepository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            if (addResult.IsFailure)
            {
                return Result<BrandResponse>.Failure(addResult.Error!);
            }

            auditContext.Set(new AuditContextEntry(nameof(Brand), entity.Id.ToString(), AuditAction.Created));
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<BrandResponse>.Success(BrandResponse.FromEntity(entity));
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            return Result<BrandResponse>.Failure(Error.Conflict(exception.Message));
        }
    }
}
