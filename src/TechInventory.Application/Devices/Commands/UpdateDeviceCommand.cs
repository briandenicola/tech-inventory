using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.Domain.ValueObjects;

namespace TechInventory.Application.Devices.Commands;

public sealed record UpdateDeviceCommand(
    Guid Id,
    string Name,
    Guid BrandId,
    Guid CategoryId,
    Guid OwnerId,
    Guid LocationId,
    string CurrencyCode,
    string? Model = null,
    string? SerialNumber = null,
    Guid? NetworkId = null,
    DateOnly? PurchaseDate = null,
    decimal? PurchasePrice = null,
    DeviceStatus Status = DeviceStatus.Active,
    string? Notes = null,
    DateOnly? RetiredDate = null,
    string? DisposalMethod = null) : IRequest<Result<DeviceResponse>>, IAuditable;

public sealed class UpdateDeviceCommandHandler(
    IDeviceRepository deviceRepository,
    IBrandRepository brandRepository,
    ICategoryRepository categoryRepository,
    IOwnerRepository ownerRepository,
    ILocationRepository locationRepository,
    INetworkRepository networkRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<UpdateDeviceCommand, Result<DeviceResponse>>
{
    public async Task<Result<DeviceResponse>> Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
    {
        var deviceResult = await deviceRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (deviceResult.IsFailure)
        {
            return Result<DeviceResponse>.Failure(deviceResult.Error!);
        }

        var device = deviceResult.Value!;
        if (device.Status == DeviceStatus.Disposed)
        {
            return Result<DeviceResponse>.Failure(Error.Conflict($"Device '{request.Id}' is already disposed."));
        }

        var referenceError = await ValidateActiveReferencesAsync(request, cancellationToken).ConfigureAwait(false);
        if (referenceError is not null)
        {
            return Result<DeviceResponse>.Failure(referenceError);
        }

        var beforeSnapshot = DeviceResponse.FromEntity(device);

        try
        {
            ApplyChanges(device, request);
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

    private async Task<Error?> ValidateActiveReferencesAsync(UpdateDeviceCommand request, CancellationToken cancellationToken)
    {
        var brandResult = await brandRepository.GetByIdAsync(request.BrandId, cancellationToken).ConfigureAwait(false);
        if (brandResult.IsFailure)
        {
            return brandResult.Error;
        }

        if (!brandResult.Value!.IsActive)
        {
            return Error.Conflict($"Brand '{request.BrandId}' is inactive.");
        }

        var categoryResult = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken).ConfigureAwait(false);
        if (categoryResult.IsFailure)
        {
            return categoryResult.Error;
        }

        if (!categoryResult.Value!.IsActive)
        {
            return Error.Conflict($"Category '{request.CategoryId}' is inactive.");
        }

        var ownerResult = await ownerRepository.GetByIdAsync(request.OwnerId, cancellationToken).ConfigureAwait(false);
        if (ownerResult.IsFailure)
        {
            return ownerResult.Error;
        }

        if (!ownerResult.Value!.IsActive)
        {
            return Error.Conflict($"Owner '{request.OwnerId}' is inactive.");
        }

        var locationResult = await locationRepository.GetByIdAsync(request.LocationId, cancellationToken).ConfigureAwait(false);
        if (locationResult.IsFailure)
        {
            return locationResult.Error;
        }

        if (!locationResult.Value!.IsActive)
        {
            return Error.Conflict($"Location '{request.LocationId}' is inactive.");
        }

        if (!request.NetworkId.HasValue)
        {
            return null;
        }

        var networkResult = await networkRepository.GetByIdAsync(request.NetworkId.Value, cancellationToken).ConfigureAwait(false);
        if (networkResult.IsFailure)
        {
            return networkResult.Error;
        }

        return networkResult.Value!.IsActive
            ? null
            : Error.Conflict($"Network '{request.NetworkId.Value}' is inactive.");
    }

    private static void ApplyChanges(Device device, UpdateDeviceCommand request)
    {
        var targetCurrency = Currency.From(request.CurrencyCode);
        if (device.Status == DeviceStatus.Retired)
        {
            EnsureRetiredDeviceMutationIsSafe(device, request, targetCurrency);

            device.UpdateNotes(request.Notes);
            if (!string.Equals(device.DisposalMethod, NormalizeOptional(request.DisposalMethod), StringComparison.Ordinal))
            {
                device.UpdateDisposalMethod(request.DisposalMethod);
            }

            if (request.Status == DeviceStatus.Disposed)
            {
                device.ChangeStatus(DeviceStatus.Disposed, device.RetiredDate, request.DisposalMethod);
            }

            return;
        }

        device.UpdateDetails(
            request.Name,
            request.BrandId,
            request.CategoryId,
            request.OwnerId,
            request.LocationId,
            targetCurrency,
            request.Model,
            request.SerialNumber,
            request.NetworkId,
            request.PurchaseDate,
            request.PurchasePrice);

        if (request.Status != device.Status || request.RetiredDate != device.RetiredDate || NormalizeOptional(request.DisposalMethod) != device.DisposalMethod)
        {
            device.ChangeStatus(request.Status, request.RetiredDate, request.DisposalMethod);
        }

        device.UpdateNotes(request.Notes);
    }

    private static void EnsureRetiredDeviceMutationIsSafe(Device device, UpdateDeviceCommand request, Currency targetCurrency)
    {
        if (request.Status is not DeviceStatus.Retired and not DeviceStatus.Disposed)
        {
            throw new InvalidOperationException("Retired devices are read-only except for notes and disposal method.");
        }

        var immutableFieldsChanged = !string.Equals(device.Name, request.Name?.Trim(), StringComparison.Ordinal)
            || device.BrandId != request.BrandId
            || device.CategoryId != request.CategoryId
            || device.OwnerId != request.OwnerId
            || device.LocationId != request.LocationId
            || !string.Equals(device.Currency.Code, targetCurrency.Code, StringComparison.Ordinal)
            || !string.Equals(device.Model, NormalizeOptional(request.Model), StringComparison.Ordinal)
            || !string.Equals(device.SerialNumber, NormalizeOptional(request.SerialNumber), StringComparison.Ordinal)
            || device.NetworkId != request.NetworkId
            || device.PurchaseDate != request.PurchaseDate
            || device.PurchasePrice != request.PurchasePrice
            || device.RetiredDate != request.RetiredDate;

        if (immutableFieldsChanged)
        {
            throw new InvalidOperationException("Retired devices are read-only except for notes and disposal method.");
        }
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
