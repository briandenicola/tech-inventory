using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Owners.Queries;

public sealed record GetOwnerByIdQuery(Guid Id) : IRequest<Result<OwnerResponse>>;

public sealed class GetOwnerByIdQueryHandler(IOwnerRepository ownerRepository) : IRequestHandler<GetOwnerByIdQuery, Result<OwnerResponse>>
{
    public async Task<Result<OwnerResponse>> Handle(GetOwnerByIdQuery request, CancellationToken cancellationToken)
    {
        var ownerResult = await ownerRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return ownerResult.IsFailure
            ? Result<OwnerResponse>.Failure(ownerResult.Error!)
            : Result<OwnerResponse>.Success(OwnerResponse.FromEntity(ownerResult.Value!));
    }
}
