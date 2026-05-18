using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Devices.Commands;

public sealed record DeleteDeviceCommand(Guid Id, string? DisposalMethod = null, DateOnly? RetiredDate = null) : IRequest<Result>, IAuditable;

public sealed class DeleteDeviceCommandHandler(
    IDeviceRepository deviceRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<DeleteDeviceCommand, Result>
{
    public async Task<Result> Handle(DeleteDeviceCommand request, CancellationToken cancellationToken)
    {
        var deviceResult = await deviceRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (deviceResult.IsFailure)
        {
            return Result.Failure(deviceResult.Error!);
        }

        var device = deviceResult.Value!;
        if (device.Status == DeviceStatus.Disposed)
        {
            return Result.Failure(Error.Conflict($"Device '{request.Id}' is already disposed."));
        }

        var beforeSnapshot = DeviceResponse.FromEntity(device);

        try
        {
            device.ChangeStatus(DeviceStatus.Disposed, request.RetiredDate ?? device.RetiredDate, request.DisposalMethod ?? device.DisposalMethod);
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            return Result.Failure(Error.Conflict(exception.Message));
        }

        var updateResult = await deviceRepository.UpdateAsync(device, cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure)
        {
            return Result.Failure(updateResult.Error!);
        }

        auditContext.Set(new AuditContextEntry(nameof(Device), device.Id.ToString(), AuditAction.Deleted, beforePayload: beforeSnapshot));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
