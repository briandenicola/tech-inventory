using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Networks.Queries;

public sealed record GetNetworkByIdQuery(Guid Id) : IRequest<Result<NetworkResponse>>;

public sealed class GetNetworkByIdQueryHandler(INetworkRepository networkRepository) : IRequestHandler<GetNetworkByIdQuery, Result<NetworkResponse>>
{
    public async Task<Result<NetworkResponse>> Handle(GetNetworkByIdQuery request, CancellationToken cancellationToken)
    {
        var entityResult = await networkRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return entityResult.IsFailure
            ? Result<NetworkResponse>.Failure(entityResult.Error!)
            : Result<NetworkResponse>.Success(NetworkResponse.FromEntity(entityResult.Value!));
    }
}
