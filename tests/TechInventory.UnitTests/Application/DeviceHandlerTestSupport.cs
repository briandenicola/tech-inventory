using FluentValidation;
using MediatR;
using TechInventory.Application.Behaviors;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Devices;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.Domain.ValueObjects;

namespace TechInventory.UnitTests.Application;

internal static class DeviceHandlerTestSupport
{
    public static Household CreateHousehold() => new(Guid.NewGuid(), "Nostromo", Currency.From("USD"));

    public static Brand CreateBrand(bool isActive = true)
    {
        var brand = new Brand(Guid.NewGuid(), "Lenovo");
        if (!isActive)
        {
            brand.Deactivate();
        }

        return brand;
    }

    public static Category CreateCategory(bool isActive = true)
    {
        var category = new Category(Guid.NewGuid(), "Computers");
        if (!isActive)
        {
            category.Deactivate();
        }

        return category;
    }

    public static Owner CreateOwner(bool isActive = true)
    {
        var owner = new Owner(Guid.NewGuid(), "Ripley");
        if (!isActive)
        {
            owner.Deactivate();
        }

        return owner;
    }

    public static Location CreateLocation(bool isActive = true)
    {
        var location = new Location(Guid.NewGuid(), "Desk", LocationType.Home);
        if (!isActive)
        {
            location.Deactivate();
        }

        return location;
    }

    public static Network CreateNetwork(bool isActive = true)
    {
        var network = new Network(Guid.NewGuid(), "Home LAN");
        if (!isActive)
        {
            network.Deactivate();
        }

        return network;
    }

    public static Tag CreateTag(bool isActive = true)
    {
        var tag = new Tag(Guid.NewGuid(), "Network");
        if (!isActive)
        {
            tag.Deactivate();
        }

        return tag;
    }

    public static Device CreateDevice(
        Guid? brandId = null,
        Guid? categoryId = null,
        Guid? ownerId = null,
        Guid? locationId = null,
        Guid? networkId = null,
        DeviceStatus status = DeviceStatus.Active)
    {
        DateOnly? retiredDate = status is DeviceStatus.Retired or DeviceStatus.Disposed
            ? DateOnly.FromDateTime(DateTime.UtcNow)
            : null;
        var disposalMethod = status == DeviceStatus.Disposed ? "Recycle" : null;

        return new Device(
            Guid.NewGuid(),
            "Family Laptop",
            brandId ?? Guid.NewGuid(),
            categoryId ?? Guid.NewGuid(),
            ownerId ?? Guid.NewGuid(),
            locationId ?? Guid.NewGuid(),
            Currency.From("USD"),
            model: "ThinkPad",
            serialNumber: "SN-42",
            networkId: networkId,
            purchaseDate: new DateOnly(2024, 1, 15),
            purchasePrice: 1200m,
            status: status,
            notes: "Personal machine",
            retiredDate: retiredDate,
            disposalMethod: disposalMethod);
    }

    public static DeviceResponse SampleDeviceResponse()
        => DeviceResponse.FromEntity(CreateDevice());

    public static DeviceTagResponse SampleDeviceTagResponse()
        => DeviceTagResponse.FromEntity(new DeviceTag(Guid.NewGuid(), Guid.NewGuid()));

    public static async Task<Result<TResponse>> ValidateAsync<TRequest, TResponse>(
        TRequest request,
        IValidator<TRequest> validator,
        TResponse successValue)
        where TRequest : IRequest<Result<TResponse>>
    {
        var behavior = new ValidationBehavior<TRequest, Result<TResponse>>([validator]);

        return await behavior.Handle(
            request,
            _ => Task.FromResult(Result<TResponse>.Success(successValue)),
            CancellationToken.None);
    }

    public static async Task<Result> ValidateAsync<TRequest>(TRequest request, IValidator<TRequest> validator)
        where TRequest : IRequest<Result>
    {
        var behavior = new ValidationBehavior<TRequest, Result>([validator]);

        return await behavior.Handle(
            request,
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);
    }
}
