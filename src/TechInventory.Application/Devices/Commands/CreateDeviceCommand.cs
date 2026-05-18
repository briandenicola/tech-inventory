using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.Domain.ValueObjects;

namespace TechInventory.Application.Devices.Commands;

public sealed record CreateDeviceCommand(
    string Name,
    Guid? BrandId,
    Guid CategoryId,
    Guid OwnerId,
    Guid LocationId,
    string? CurrencyCode = null,
    string? Model = null,
    string? SerialNumber = null,
    Guid? NetworkId = null,
    DateOnly? PurchaseDate = null,
    decimal? PurchasePrice = null,
    DeviceStatus Status = DeviceStatus.Active,
    string? Notes = null,
    DateOnly? RetiredDate = null,
    string? DisposalMethod = null,
    string? Purpose = null,
    string? OperatingSystem = null,
    string? IpAddress = null,
    string? MacAddress = null,
    string? ProductUrl = null,
    string? Version = null) : IRequest<Result<DeviceResponse>>, IAuditable;

public sealed class CreateDeviceCommandHandler(
    IDeviceRepository deviceRepository,
    IBrandRepository brandRepository,
    ICategoryRepository categoryRepository,
    IOwnerRepository ownerRepository,
    ILocationRepository locationRepository,
    INetworkRepository networkRepository,
    IHouseholdRepository householdRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<CreateDeviceCommand, Result<DeviceResponse>>
{
    public async Task<Result<DeviceResponse>> Handle(CreateDeviceCommand request, CancellationToken cancellationToken)
    {
        var referenceError = await ValidateActiveReferencesAsync(request, cancellationToken).ConfigureAwait(false);
        if (referenceError is not null)
        {
            return Result<DeviceResponse>.Failure(referenceError);
        }

        var householdResult = await ResolveSingleHouseholdAsync(cancellationToken).ConfigureAwait(false);
        if (householdResult.IsFailure)
        {
            return Result<DeviceResponse>.Failure(householdResult.Error!);
        }

        try
        {
            var device = Device.Create(
                Guid.NewGuid(),
                householdResult.Value!,
                request.Name,
                request.BrandId,
                request.CategoryId,
                request.OwnerId,
                request.LocationId,
                request.Model,
                request.SerialNumber,
                request.NetworkId,
                request.PurchaseDate,
                request.PurchasePrice,
                string.IsNullOrWhiteSpace(request.CurrencyCode) ? null : Currency.From(request.CurrencyCode),
                request.Status,
                request.Notes,
                request.RetiredDate,
                request.DisposalMethod,
                request.Purpose,
                request.OperatingSystem,
                request.IpAddress,
                request.MacAddress,
                request.ProductUrl,
                request.Version);

            var addResult = await deviceRepository.AddAsync(device, cancellationToken).ConfigureAwait(false);
            if (addResult.IsFailure)
            {
                return Result<DeviceResponse>.Failure(addResult.Error!);
            }

            auditContext.Set(new AuditContextEntry(nameof(Device), device.Id.ToString(), AuditAction.Created));
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<DeviceResponse>.Success(DeviceResponse.FromEntity(device));
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            return Result<DeviceResponse>.Failure(Error.Conflict(exception.Message));
        }
    }

    private async Task<Result<Household>> ResolveSingleHouseholdAsync(CancellationToken cancellationToken)
    {
        var households = await householdRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        return households.Count switch
        {
            1 => Result<Household>.Success(households[0]),
            0 => Result<Household>.Failure(Error.Conflict("A household must exist before creating a device.")),
            _ => Result<Household>.Failure(Error.Conflict("CreateDeviceCommand requires exactly one household.")),
        };
    }

    private async Task<Error?> ValidateActiveReferencesAsync(CreateDeviceCommand request, CancellationToken cancellationToken)
    {
        if (request.BrandId.HasValue)
        {
            var brandResult = await brandRepository.GetByIdAsync(request.BrandId.Value, cancellationToken).ConfigureAwait(false);
            if (brandResult.IsFailure)
            {
                return brandResult.Error;
            }

            if (!brandResult.Value!.IsActive)
            {
                return Error.Conflict($"Brand '{request.BrandId.Value}' is inactive.");
            }
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
}
