using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Brands;
using TechInventory.Application.BulkOperations;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Brands.Commands;

public sealed record BulkDeleteBrandsCommand(IReadOnlyList<Guid> BrandIds)
    : IRequest<Result<BulkOperationResponse>>, IAuditable, IBulkDeleteReferenceEntityCommand
{
    public IReadOnlyList<Guid> Ids => BrandIds;
}

public sealed class BulkDeleteBrandsCommandHandler(
    IBrandRepository brandRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<BulkDeleteBrandsCommand, Result<BulkOperationResponse>>
{
    public async Task<Result<BulkOperationResponse>> Handle(BulkDeleteBrandsCommand request, CancellationToken cancellationToken)
    {
        var uniqueIds = request.BrandIds.Distinct().ToArray();
        var brands = new List<Brand>(uniqueIds.Length);

        foreach (var brandId in uniqueIds)
        {
            var brandResult = await brandRepository.GetByIdAsync(brandId, cancellationToken).ConfigureAwait(false);
            if (brandResult.IsFailure)
            {
                return Result<BulkOperationResponse>.Failure(brandResult.Error!);
            }

            var brand = brandResult.Value!;
            if (!brand.IsActive)
            {
                return Result<BulkOperationResponse>.Failure(Error.Conflict($"Brand '{brandId}' is already inactive."));
            }

            brands.Add(brand);
        }

        var correlationId = Guid.NewGuid();
        foreach (var brand in brands)
        {
            var beforeSnapshot = BrandResponse.FromEntity(brand);
            brand.Deactivate();

            var updateResult = await brandRepository.UpdateAsync(brand, cancellationToken).ConfigureAwait(false);
            if (updateResult.IsFailure)
            {
                return Result<BulkOperationResponse>.Failure(updateResult.Error!);
            }

            auditContext.Add(new AuditContextEntry(
                nameof(Brand),
                brand.Id.ToString(),
                AuditAction.Deleted,
                beforePayload: new BulkAuditEnvelope(correlationId, beforeSnapshot),
                afterPayload: new BulkAuditEnvelope(correlationId, BrandResponse.FromEntity(brand))));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<BulkOperationResponse>.Success(new BulkOperationResponse(correlationId, brands.Count));
    }
}
