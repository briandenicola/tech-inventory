using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Locations.Queries;

public sealed record GetLocationByIdQuery(Guid Id) : IRequest<Result<LocationResponse>>;

public sealed class GetLocationByIdQueryHandler(ILocationRepository locationRepository) : IRequestHandler<GetLocationByIdQuery, Result<LocationResponse>>
{
    public async Task<Result<LocationResponse>> Handle(GetLocationByIdQuery request, CancellationToken cancellationToken)
    {
        var entityResult = await locationRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return entityResult.IsFailure
            ? Result<LocationResponse>.Failure(entityResult.Error!)
            : Result<LocationResponse>.Success(LocationResponse.FromEntity(entityResult.Value!));
    }
}
