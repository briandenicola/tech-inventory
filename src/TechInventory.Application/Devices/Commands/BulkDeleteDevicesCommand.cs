using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.BulkOperations;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Devices.Commands;

/// <summary>
/// F024 — soft-delete every device referenced by <see cref="DeviceIds"/> in
/// one transaction with a shared deletion reason. Mirrors single-device
/// soft-delete semantics (status → Disposed) but emits one AuditEvent per
/// affected device sharing a single CorrelationId.
/// </summary>
public sealed record BulkDeleteDevicesCommand(
    IReadOnlyList<Guid> DeviceIds,
    string Reason) : IRequest<Result<BulkOperationResponse>>, IAuditable;

public sealed class BulkDeleteDevicesCommandHandler(
    IDeviceRepository deviceRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<BulkDeleteDevicesCommand, Result<BulkOperationResponse>>
{
    public async Task<Result<BulkOperationResponse>> Handle(BulkDeleteDevicesCommand request, CancellationToken cancellationToken)
    {
        var uniqueIds = request.DeviceIds.Distinct().ToArray();
        var correlationId = Guid.NewGuid();
        var trimmedReason = request.Reason.Trim();
        var affected = 0;

        foreach (var deviceId in uniqueIds)
        {
            var deviceResult = await deviceRepository.GetByIdAsync(deviceId, cancellationToken).ConfigureAwait(false);
            if (deviceResult.IsFailure)
            {
                return Result<BulkOperationResponse>.Failure(deviceResult.Error!);
            }

            var device = deviceResult.Value!;
            if (device.Status == DeviceStatus.Disposed)
            {
                return Result<BulkOperationResponse>.Failure(Error.Conflict($"Device '{deviceId}' is already disposed."));
            }

            var beforeSnapshot = DeviceResponse.FromEntity(device);

            try
            {
                device.ChangeStatus(DeviceStatus.Disposed, device.RetiredDate, device.DisposalMethod);
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                return Result<BulkOperationResponse>.Failure(Error.Conflict($"Device '{deviceId}': {exception.Message}"));
            }

            var updateResult = await deviceRepository.UpdateAsync(device, cancellationToken).ConfigureAwait(false);
            if (updateResult.IsFailure)
            {
                return Result<BulkOperationResponse>.Failure(updateResult.Error!);
            }

            auditContext.Add(new AuditContextEntry(
                nameof(Device),
                device.Id.ToString(),
                AuditAction.Deleted,
                beforePayload: new BulkAuditEnvelope(correlationId, beforeSnapshot),
                afterPayload: new BulkDeleteAfterPayload(correlationId, trimmedReason, DeviceResponse.FromEntity(device))));

            affected++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<BulkOperationResponse>.Success(new BulkOperationResponse(correlationId, affected));
    }
}

internal sealed record BulkDeleteAfterPayload(Guid CorrelationId, string Reason, object Payload);
