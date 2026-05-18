using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Brands.Queries;

public sealed record GetBrandByIdQuery(Guid Id) : IRequest<Result<BrandResponse>>;

public sealed class GetBrandByIdQueryHandler(IBrandRepository brandRepository) : IRequestHandler<GetBrandByIdQuery, Result<BrandResponse>>
{
    public async Task<Result<BrandResponse>> Handle(GetBrandByIdQuery request, CancellationToken cancellationToken)
    {
        var entityResult = await brandRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return entityResult.IsFailure
            ? Result<BrandResponse>.Failure(entityResult.Error!)
            : Result<BrandResponse>.Success(BrandResponse.FromEntity(entityResult.Value!));
    }
}
