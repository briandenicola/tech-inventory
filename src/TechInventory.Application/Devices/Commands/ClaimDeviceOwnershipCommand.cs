using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Devices.Commands;

public sealed record ClaimDeviceOwnershipCommand(Guid DeviceId, Guid OwnerId) : IRequest<Result<DeviceResponse>>, IAuditable;

public sealed class ClaimDeviceOwnershipCommandHandler(
    IDeviceRepository deviceRepository,
    IOwnerRepository ownerRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<ClaimDeviceOwnershipCommand, Result<DeviceResponse>>
{
    public async Task<Result<DeviceResponse>> Handle(ClaimDeviceOwnershipCommand request, CancellationToken cancellationToken)
    {
        var deviceResult = await deviceRepository.GetByIdAsync(request.DeviceId, cancellationToken).ConfigureAwait(false);
        if (deviceResult.IsFailure)
        {
            return Result<DeviceResponse>.Failure(deviceResult.Error!);
        }

        var device = deviceResult.Value!;
        if (device.Status == DeviceStatus.Disposed)
        {
            return Result<DeviceResponse>.Failure(Error.Conflict($"Device '{request.DeviceId}' is disposed."));
        }

        if (device.OwnerId == request.OwnerId)
        {
            return Result<DeviceResponse>.Failure(Error.Conflict($"Device '{request.DeviceId}' is already assigned to owner '{request.OwnerId}'."));
        }

        var ownerResult = await ownerRepository.GetByIdAsync(request.OwnerId, cancellationToken).ConfigureAwait(false);
        if (ownerResult.IsFailure)
        {
            return Result<DeviceResponse>.Failure(ownerResult.Error!);
        }

        if (!ownerResult.Value!.IsActive)
        {
            return Result<DeviceResponse>.Failure(Error.Conflict($"Owner '{request.OwnerId}' is inactive."));
        }

        var beforeSnapshot = new DeviceOwnershipAuditSnapshot(device.Id, device.OwnerId);

        try
        {
            device.UpdateDetails(
                device.Name,
                device.BrandId,
                device.CategoryId,
                request.OwnerId,
                device.LocationId,
                device.Currency,
                device.Model,
                device.SerialNumber,
                device.NetworkId,
                device.PurchaseDate,
                device.PurchasePrice);
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            return Result<DeviceResponse>.Failure(Error.Conflict(exception.Message));
        }

        var updateResult = await deviceRepository.UpdateAsync(device, cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure)
        {
            return Result<DeviceResponse>.Failure(updateResult.Error!);
        }

        auditContext.Set(new AuditContextEntry(nameof(Device), device.Id.ToString(), AuditAction.Updated, beforePayload: beforeSnapshot));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<DeviceResponse>.Success(DeviceResponse.FromEntity(device));
    }

    private sealed record DeviceOwnershipAuditSnapshot(Guid DeviceId, Guid OwnerId);
}
