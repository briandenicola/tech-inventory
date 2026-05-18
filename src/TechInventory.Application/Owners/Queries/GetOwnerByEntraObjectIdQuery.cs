using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Owners.Queries;

public sealed record GetOwnerByEntraObjectIdQuery(Guid EntraObjectId) : IRequest<Result<OwnerResponse>>;

public sealed class GetOwnerByEntraObjectIdQueryHandler(IOwnerRepository ownerRepository)
    : IRequestHandler<GetOwnerByEntraObjectIdQuery, Result<OwnerResponse>>
{
    public async Task<Result<OwnerResponse>> Handle(GetOwnerByEntraObjectIdQuery request, CancellationToken cancellationToken)
    {
        var ownerResult = await ownerRepository.GetByEntraObjectIdAsync(request.EntraObjectId, cancellationToken).ConfigureAwait(false);
        return ownerResult.IsFailure
            ? Result<OwnerResponse>.Failure(ownerResult.Error!)
            : Result<OwnerResponse>.Success(OwnerResponse.FromEntity(ownerResult.Value!));
    }
}
