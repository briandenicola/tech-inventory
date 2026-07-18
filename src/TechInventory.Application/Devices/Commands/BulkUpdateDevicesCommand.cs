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
/// F024 — apply a single set of field updates to every device referenced by
/// <see cref="DeviceIds"/> in one atomic transaction. Any null field on
/// <see cref="Changes"/> is left untouched; at least one field must be set
/// (enforced by the validator). All affected devices share a single
/// CorrelationId that is embedded in each emitted AuditEvent's after-payload
/// so the bulk operation can be reconstructed from the append-only audit log.
/// </summary>
public sealed record BulkUpdateDevicesCommand(
    IReadOnlyList<Guid> DeviceIds,
    BulkUpdateDeviceChanges Changes) : IRequest<Result<BulkOperationResponse>>, IAuditable;

public sealed record BulkUpdateDeviceChanges(
    Guid? CategoryId = null,
    Guid? OwnerId = null,
    Guid? BrandId = null,
    Guid? LocationId = null,
    DeviceStatus? Status = null);

public sealed class BulkUpdateDevicesCommandHandler(
    IDeviceRepository deviceRepository,
    IBrandRepository brandRepository,
    ICategoryRepository categoryRepository,
    IOwnerRepository ownerRepository,
    ILocationRepository locationRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<BulkUpdateDevicesCommand, Result<BulkOperationResponse>>
{
    public async Task<Result<BulkOperationResponse>> Handle(BulkUpdateDevicesCommand request, CancellationToken cancellationToken)
    {
        var uniqueIds = request.DeviceIds.Distinct().ToArray();

        // Validate reference entities up-front (single round-trip per ref) so the
        // batch fails before mutating any rows when, e.g., the target category
        // is inactive or missing.
        var referenceError = await ValidateReferenceEntitiesAsync(request.Changes, cancellationToken).ConfigureAwait(false);
        if (referenceError is not null)
        {
            return Result<BulkOperationResponse>.Failure(referenceError);
        }

        var correlationId = Guid.NewGuid();
        var devices = new List<Device>(uniqueIds.Length);

        // Single round trip for every targeted device, rather than one SELECT
        // per id — see #95.
        var devicesById = (await deviceRepository.GetByIdsAsync(uniqueIds, cancellationToken).ConfigureAwait(false))
            .ToDictionary(device => device.Id);

        // First pass: load + apply changes in-memory. Any single failure aborts
        // the whole batch before SaveChanges is called.
        foreach (var deviceId in uniqueIds)
        {
            if (!devicesById.TryGetValue(deviceId, out var device))
            {
                return Result<BulkOperationResponse>.Failure(Error.NotFound($"Device '{deviceId}' was not found."));
            }

            if (device.Status == DeviceStatus.Disposed)
            {
                return Result<BulkOperationResponse>.Failure(Error.Conflict($"Device '{deviceId}' is already disposed."));
            }

            var beforeSnapshot = DeviceResponse.FromEntity(device);

            try
            {
                ApplyChanges(device, request.Changes);
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

            var afterSnapshot = DeviceResponse.FromEntity(device);
            auditContext.Add(new AuditContextEntry(
                nameof(Device),
                device.Id.ToString(),
                AuditAction.Updated,
                beforePayload: new BulkAuditEnvelope(correlationId, beforeSnapshot),
                afterPayload: new BulkAuditEnvelope(correlationId, afterSnapshot)));

            devices.Add(device);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<BulkOperationResponse>.Success(new BulkOperationResponse(correlationId, devices.Count));
    }

    private async Task<Error?> ValidateReferenceEntitiesAsync(BulkUpdateDeviceChanges changes, CancellationToken cancellationToken)
    {
        if (changes.BrandId.HasValue)
        {
            var brandResult = await brandRepository.GetByIdAsync(changes.BrandId.Value, cancellationToken).ConfigureAwait(false);
            if (brandResult.IsFailure)
            {
                return brandResult.Error;
            }

            if (!brandResult.Value!.IsActive)
            {
                return Error.Conflict($"Brand '{changes.BrandId.Value}' is inactive.");
            }
        }

        if (changes.CategoryId.HasValue)
        {
            var categoryResult = await categoryRepository.GetByIdAsync(changes.CategoryId.Value, cancellationToken).ConfigureAwait(false);
            if (categoryResult.IsFailure)
            {
                return categoryResult.Error;
            }

            if (!categoryResult.Value!.IsActive)
            {
                return Error.Conflict($"Category '{changes.CategoryId.Value}' is inactive.");
            }
        }

        if (changes.OwnerId.HasValue)
        {
            var ownerResult = await ownerRepository.GetByIdAsync(changes.OwnerId.Value, cancellationToken).ConfigureAwait(false);
            if (ownerResult.IsFailure)
            {
                return ownerResult.Error;
            }

            if (!ownerResult.Value!.IsActive)
            {
                return Error.Conflict($"Owner '{changes.OwnerId.Value}' is inactive.");
            }
        }

        if (changes.LocationId.HasValue)
        {
            var locationResult = await locationRepository.GetByIdAsync(changes.LocationId.Value, cancellationToken).ConfigureAwait(false);
            if (locationResult.IsFailure)
            {
                return locationResult.Error;
            }

            if (!locationResult.Value!.IsActive)
            {
                return Error.Conflict($"Location '{changes.LocationId.Value}' is inactive.");
            }
        }

        return null;
    }

    private static void ApplyChanges(Device device, BulkUpdateDeviceChanges changes)
    {
        if (device.Status == DeviceStatus.Retired)
        {
            ApplyRetiredDeviceChanges(device, changes);
            return;
        }

        var targetBrand = changes.BrandId ?? device.BrandId;
        var targetCategory = changes.CategoryId ?? device.CategoryId;
        var targetOwner = changes.OwnerId ?? device.OwnerId;
        var targetLocation = changes.LocationId ?? device.LocationId;

        device.UpdateDetails(
            device.Name,
            targetBrand,
            targetCategory,
            targetOwner,
            targetLocation,
            device.Currency,
            device.Model,
            device.SerialNumber,
            device.NetworkId,
            device.PurchaseDate,
            device.PurchasePrice,
            null,
            device.Purpose,
            device.OperatingSystem,
            device.IpAddress,
            device.MacAddress,
            device.ProductUrl,
            device.Version);

        if (changes.Status is { } targetStatus && targetStatus != device.Status)
        {
            device.ChangeStatus(targetStatus, device.RetiredDate, device.DisposalMethod);
        }
    }

    private static void ApplyRetiredDeviceChanges(Device device, BulkUpdateDeviceChanges changes)
    {
        if (changes.BrandId.HasValue || changes.CategoryId.HasValue || changes.OwnerId.HasValue || changes.LocationId.HasValue)
        {
            throw new InvalidOperationException("Retired devices are read-only except for status changes.");
        }

        switch (changes.Status)
        {
            case DeviceStatus.Active:
                device.Reactivate();
                return;
            case DeviceStatus.Disposed:
                device.ChangeStatus(DeviceStatus.Disposed, device.RetiredDate, device.DisposalMethod);
                return;
            case DeviceStatus.Retired:
            case null:
                return;
            default:
                throw new InvalidOperationException("Retired devices can only be moved back to Active or disposed via bulk update.");
        }
    }
}
