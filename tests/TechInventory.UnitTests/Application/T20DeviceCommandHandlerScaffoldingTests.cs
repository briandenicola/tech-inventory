using FluentAssertions;
using NSubstitute;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Devices;
using TechInventory.Application.Devices.Commands;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.UnitTests.Application;

public sealed class T20DeviceCommandHandlerScaffoldingTests
{
    [Fact]
    public async Task CreateDeviceCommandHandler_WhenValidInput_ReturnsSuccess()
    {
        var dependencies = CreateDependencies();
        var brand = DeviceHandlerTestSupport.CreateBrand();
        var category = DeviceHandlerTestSupport.CreateCategory();
        var owner = DeviceHandlerTestSupport.CreateOwner();
        var location = DeviceHandlerTestSupport.CreateLocation();
        var network = DeviceHandlerTestSupport.CreateNetwork();
        var household = DeviceHandlerTestSupport.CreateHousehold();
        var command = new CreateDeviceCommand("Updated Laptop", brand.Id, category.Id, owner.Id, location.Id, NetworkId: network.Id);

        ArrangeActiveReferences(dependencies, brand, category, owner, location, network);
        dependencies.HouseholdRepository.ListAsync(Arg.Any<CancellationToken>()).Returns([household]);
        dependencies.DeviceRepository
            .AddAsync(Arg.Any<Device>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<Device>.Success(call.Arg<Device>()));
        dependencies.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = CreateCreateHandler(dependencies);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Laptop");
        result.Value.CurrencyCode.Should().Be("USD");
        await dependencies.DeviceRepository.Received(1).AddAsync(
            Arg.Is<Device>(device =>
                device.Name == "Updated Laptop" &&
                device.BrandId == brand.Id &&
                device.CategoryId == category.Id &&
                device.OwnerId == owner.Id &&
                device.LocationId == location.Id &&
                device.NetworkId == network.Id &&
                device.Currency.Code == "USD"),
            Arg.Any<CancellationToken>());
        dependencies.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Device) &&
            entry.Action == AuditAction.Created));
        await dependencies.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDeviceCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var command = new CreateDeviceCommand(string.Empty, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, CurrencyCode: "ZZZ");

        var result = await DeviceHandlerTestSupport.ValidateAsync(command, new CreateDeviceCommandValidator(), DeviceHandlerTestSupport.SampleDeviceResponse());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task CreateDeviceCommandHandler_WhenRepositoryDetectsAConflict_ReturnsConflictFailure()
    {
        var dependencies = CreateDependencies();
        var brand = DeviceHandlerTestSupport.CreateBrand();
        var category = DeviceHandlerTestSupport.CreateCategory();
        var owner = DeviceHandlerTestSupport.CreateOwner();
        var location = DeviceHandlerTestSupport.CreateLocation();
        var household = DeviceHandlerTestSupport.CreateHousehold();

        ArrangeActiveReferences(dependencies, brand, category, owner, location);
        dependencies.HouseholdRepository.ListAsync(Arg.Any<CancellationToken>()).Returns([household]);
        dependencies.DeviceRepository
            .AddAsync(Arg.Any<Device>(), Arg.Any<CancellationToken>())
            .Returns(Result<Device>.Failure(Error.Conflict("Duplicate device.")));
        var handler = CreateCreateHandler(dependencies);

        var result = await handler.Handle(new CreateDeviceCommand("Laptop", brand.Id, category.Id, owner.Id, location.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task UpdateDeviceCommandHandler_WhenValidInput_ReturnsSuccess()
    {
        var dependencies = CreateDependencies();
        var brand = DeviceHandlerTestSupport.CreateBrand();
        var category = DeviceHandlerTestSupport.CreateCategory();
        var owner = DeviceHandlerTestSupport.CreateOwner();
        var location = DeviceHandlerTestSupport.CreateLocation();
        var network = DeviceHandlerTestSupport.CreateNetwork();
        var device = DeviceHandlerTestSupport.CreateDevice(brand.Id, category.Id, Guid.NewGuid(), location.Id);

        ArrangeActiveReferences(dependencies, brand, category, owner, location, network);
        dependencies.DeviceRepository.GetByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(Result<Device>.Success(device));
        dependencies.DeviceRepository.UpdateAsync(device, Arg.Any<CancellationToken>()).Returns(Result<Device>.Success(device));
        dependencies.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = CreateUpdateHandler(dependencies);
        var command = new UpdateDeviceCommand(device.Id, "Renamed Laptop", brand.Id, category.Id, owner.Id, location.Id, "USD", "ThinkPad X1", "SN-99", network.Id, new DateOnly(2024, 2, 1), 1500m, DeviceStatus.Active, "Updated notes");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Renamed Laptop");
        result.Value.OwnerId.Should().Be(owner.Id);
        dependencies.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.Action == AuditAction.Updated &&
            entry.BeforePayload != null &&
            entry.BeforePayload.ToString()!.Contains("Family Laptop", StringComparison.Ordinal)));
        await dependencies.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDeviceCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var command = new UpdateDeviceCommand(Guid.Empty, string.Empty, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, "ZZZ");

        var result = await DeviceHandlerTestSupport.ValidateAsync(command, new UpdateDeviceCommandValidator(), DeviceHandlerTestSupport.SampleDeviceResponse());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task UpdateDeviceCommandHandler_WhenDeviceDoesNotExist_ReturnsNotFoundFailure()
    {
        var dependencies = CreateDependencies();
        var deviceId = Guid.NewGuid();
        dependencies.DeviceRepository.GetByIdAsync(deviceId, Arg.Any<CancellationToken>()).Returns(Result<Device>.Failure(Error.NotFound("Device missing.")));
        var handler = CreateUpdateHandler(dependencies);

        var result = await handler.Handle(new UpdateDeviceCommand(deviceId, "Laptop", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "USD"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task UpdateDeviceCommandHandler_WhenDeviceIsDisposed_ReturnsConflictFailure()
    {
        var dependencies = CreateDependencies();
        var brand = DeviceHandlerTestSupport.CreateBrand();
        var category = DeviceHandlerTestSupport.CreateCategory();
        var owner = DeviceHandlerTestSupport.CreateOwner();
        var location = DeviceHandlerTestSupport.CreateLocation();
        var device = DeviceHandlerTestSupport.CreateDevice(brand.Id, category.Id, owner.Id, location.Id, status: DeviceStatus.Disposed);
        dependencies.DeviceRepository.GetByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(Result<Device>.Success(device));
        var handler = CreateUpdateHandler(dependencies);

        var result = await handler.Handle(new UpdateDeviceCommand(device.Id, "Laptop", brand.Id, category.Id, owner.Id, location.Id, "USD"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task DeleteDeviceCommandHandler_WhenDeviceExists_ReturnsSuccess()
    {
        var dependencies = CreateDependencies();
        var device = DeviceHandlerTestSupport.CreateDevice();
        dependencies.DeviceRepository.GetByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(Result<Device>.Success(device));
        dependencies.DeviceRepository.UpdateAsync(device, Arg.Any<CancellationToken>()).Returns(Result<Device>.Success(device));
        dependencies.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = CreateDeleteHandler(dependencies);

        var result = await handler.Handle(new DeleteDeviceCommand(device.Id, DisposalMethod: "Recycle"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        device.Status.Should().Be(DeviceStatus.Disposed);
        dependencies.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.Action == AuditAction.Deleted &&
            entry.BeforePayload != null &&
            entry.BeforePayload.ToString()!.Contains(device.Id.ToString(), StringComparison.Ordinal)));
        await dependencies.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteDeviceCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(new DeleteDeviceCommand(Guid.Empty), new DeleteDeviceCommandValidator());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task DeleteDeviceCommandHandler_WhenDeviceDoesNotExist_ReturnsNotFoundFailure()
    {
        var dependencies = CreateDependencies();
        var deviceId = Guid.NewGuid();
        dependencies.DeviceRepository.GetByIdAsync(deviceId, Arg.Any<CancellationToken>()).Returns(Result<Device>.Failure(Error.NotFound("Device missing.")));
        var handler = CreateDeleteHandler(dependencies);

        var result = await handler.Handle(new DeleteDeviceCommand(deviceId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task DeleteDeviceCommandHandler_WhenDeviceIsAlreadyDisposed_ReturnsConflictFailure()
    {
        var dependencies = CreateDependencies();
        var device = DeviceHandlerTestSupport.CreateDevice(status: DeviceStatus.Disposed);
        dependencies.DeviceRepository.GetByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(Result<Device>.Success(device));
        var handler = CreateDeleteHandler(dependencies);

        var result = await handler.Handle(new DeleteDeviceCommand(device.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    private static DeviceCommandDependencies CreateDependencies() => new(
        Substitute.For<IDeviceRepository>(),
        Substitute.For<IBrandRepository>(),
        Substitute.For<ICategoryRepository>(),
        Substitute.For<IOwnerRepository>(),
        Substitute.For<ILocationRepository>(),
        Substitute.For<INetworkRepository>(),
        Substitute.For<IHouseholdRepository>(),
        Substitute.For<IUnitOfWork>(),
        Substitute.For<IAuditContext>());

    private static CreateDeviceCommandHandler CreateCreateHandler(DeviceCommandDependencies dependencies)
        => new(
            dependencies.DeviceRepository,
            dependencies.BrandRepository,
            dependencies.CategoryRepository,
            dependencies.OwnerRepository,
            dependencies.LocationRepository,
            dependencies.NetworkRepository,
            dependencies.HouseholdRepository,
            dependencies.UnitOfWork,
            dependencies.AuditContext);

    private static UpdateDeviceCommandHandler CreateUpdateHandler(DeviceCommandDependencies dependencies)
        => new(
            dependencies.DeviceRepository,
            dependencies.BrandRepository,
            dependencies.CategoryRepository,
            dependencies.OwnerRepository,
            dependencies.LocationRepository,
            dependencies.NetworkRepository,
            dependencies.UnitOfWork,
            dependencies.AuditContext);

    private static DeleteDeviceCommandHandler CreateDeleteHandler(DeviceCommandDependencies dependencies)
        => new(dependencies.DeviceRepository, dependencies.UnitOfWork, dependencies.AuditContext);

    private static void ArrangeActiveReferences(
        DeviceCommandDependencies dependencies,
        Brand brand,
        Category category,
        Owner owner,
        Location location,
        Network? network = null)
    {
        dependencies.BrandRepository.GetByIdAsync(brand.Id, Arg.Any<CancellationToken>()).Returns(Result<Brand>.Success(brand));
        dependencies.CategoryRepository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(Result<Category>.Success(category));
        dependencies.OwnerRepository.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(Result<Owner>.Success(owner));
        dependencies.LocationRepository.GetByIdAsync(location.Id, Arg.Any<CancellationToken>()).Returns(Result<Location>.Success(location));

        if (network is not null)
        {
            dependencies.NetworkRepository.GetByIdAsync(network.Id, Arg.Any<CancellationToken>()).Returns(Result<Network>.Success(network));
        }
    }

    private sealed record DeviceCommandDependencies(
        IDeviceRepository DeviceRepository,
        IBrandRepository BrandRepository,
        ICategoryRepository CategoryRepository,
        IOwnerRepository OwnerRepository,
        ILocationRepository LocationRepository,
        INetworkRepository NetworkRepository,
        IHouseholdRepository HouseholdRepository,
        IUnitOfWork UnitOfWork,
        IAuditContext AuditContext);
}
