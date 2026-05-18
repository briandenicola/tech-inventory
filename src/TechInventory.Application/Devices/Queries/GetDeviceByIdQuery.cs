using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Devices.Queries;

public sealed record GetDeviceByIdQuery(Guid Id) : IRequest<Result<DeviceResponse>>;

public sealed class GetDeviceByIdQueryHandler(IDeviceRepository deviceRepository) : IRequestHandler<GetDeviceByIdQuery, Result<DeviceResponse>>
{
    public async Task<Result<DeviceResponse>> Handle(GetDeviceByIdQuery request, CancellationToken cancellationToken)
    {
        var deviceResult = await deviceRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return deviceResult.IsFailure
            ? Result<DeviceResponse>.Failure(deviceResult.Error!)
            : Result<DeviceResponse>.Success(DeviceResponse.FromEntity(deviceResult.Value!));
    }
}
