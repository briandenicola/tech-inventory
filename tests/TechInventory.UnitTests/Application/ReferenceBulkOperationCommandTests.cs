using FluentAssertions;
using NSubstitute;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Brands.Commands;
using TechInventory.Application.BulkOperations;
using TechInventory.Application.Categories.Commands;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Locations.Commands;
using TechInventory.Application.Merges;
using TechInventory.Application.Networks.Commands;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.UnitTests.Application;

public sealed class ReferenceBulkOperationCommandTests
{
    [Fact]
    public async Task BulkDeleteBrandsCommandHandler_WhenBrandsExist_DeactivatesAndWritesAuditEntries()
    {
        var deps = CreateBrandDeps();
        var first = new Brand(Guid.NewGuid(), "Apple");
        var second = new Brand(Guid.NewGuid(), "Dell");
        deps.Repository.GetByIdAsync(first.Id, Arg.Any<CancellationToken>()).Returns(Result<Brand>.Success(first));
        deps.Repository.GetByIdAsync(second.Id, Arg.Any<CancellationToken>()).Returns(Result<Brand>.Success(second));
        deps.Repository.UpdateAsync(Arg.Any<Brand>(), Arg.Any<CancellationToken>()).Returns(call => Result<Brand>.Success(call.Arg<Brand>()));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new BulkDeleteBrandsCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new BulkDeleteBrandsCommand([first.Id, second.Id]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AffectedCount.Should().Be(2);
        first.IsActive.Should().BeFalse();
        second.IsActive.Should().BeFalse();
        deps.AuditContext.Received(2).Add(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Brand) &&
            entry.Action == AuditAction.Deleted &&
            entry.BeforePayload is BulkAuditEnvelope &&
            entry.AfterPayload is BulkAuditEnvelope));
    }

    [Fact]
    public async Task BulkDeleteBrandsCommand_WhenIdsEmpty_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(
            new BulkDeleteBrandsCommand(Array.Empty<Guid>()),
            new BulkDeleteBrandsCommandValidator(),
            new BulkOperationResponse(Guid.NewGuid(), 0));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task BulkDeleteCategoriesCommandHandler_WhenParentAndChildSelected_DeletesBothWithoutDoubleUpdatingChild()
    {
        var deps = CreateCategoryDeps();
        var root = new Category(Guid.NewGuid(), "Computers");
        var child = new Category(Guid.NewGuid(), "Laptops", root.Id, 2);
        deps.Repository.ListAsync(true, Arg.Any<CancellationToken>()).Returns([root, child]);
        deps.Repository.UpdateAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>()).Returns(call => Result<Category>.Success(call.Arg<Category>()));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new BulkDeleteCategoriesCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new BulkDeleteCategoriesCommand([root.Id, child.Id]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AffectedCount.Should().Be(2);
        root.IsActive.Should().BeFalse();
        child.IsActive.Should().BeFalse();
        await deps.Repository.Received(2).UpdateAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
        deps.AuditContext.Received(2).Add(Arg.Is<AuditContextEntry>(entry => entry.EntityType == nameof(Category) && entry.Action == AuditAction.Deleted));
    }

    [Fact]
    public async Task BulkDeleteCategoriesCommand_WhenIdsEmpty_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(
            new BulkDeleteCategoriesCommand(Array.Empty<Guid>()),
            new BulkDeleteCategoriesCommandValidator(),
            new BulkOperationResponse(Guid.NewGuid(), 0));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task BulkDeleteLocationsCommandHandler_WhenLocationsExist_DeactivatesEachLocation()
    {
        var deps = CreateLocationDeps();
        var first = new Location(Guid.NewGuid(), "Desk", LocationType.Home);
        var second = new Location(Guid.NewGuid(), "Closet", LocationType.Storage);
        deps.Repository.GetByIdAsync(first.Id, Arg.Any<CancellationToken>()).Returns(Result<Location>.Success(first));
        deps.Repository.GetByIdAsync(second.Id, Arg.Any<CancellationToken>()).Returns(Result<Location>.Success(second));
        deps.Repository.UpdateAsync(Arg.Any<Location>(), Arg.Any<CancellationToken>()).Returns(call => Result<Location>.Success(call.Arg<Location>()));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new BulkDeleteLocationsCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new BulkDeleteLocationsCommand([first.Id, second.Id]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AffectedCount.Should().Be(2);
        first.IsActive.Should().BeFalse();
        second.IsActive.Should().BeFalse();
        deps.AuditContext.Received(2).Add(Arg.Is<AuditContextEntry>(entry => entry.EntityType == nameof(Location) && entry.Action == AuditAction.Deleted));
    }

    [Fact]
    public async Task BulkDeleteLocationsCommand_WhenIdsEmpty_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(
            new BulkDeleteLocationsCommand(Array.Empty<Guid>()),
            new BulkDeleteLocationsCommandValidator(),
            new BulkOperationResponse(Guid.NewGuid(), 0));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task BulkDeleteNetworksCommandHandler_WhenNetworksExist_DeactivatesEachNetwork()
    {
        var deps = CreateNetworkDeps();
        var first = new Network(Guid.NewGuid(), "Home LAN");
        var second = new Network(Guid.NewGuid(), "Guest WiFi");
        deps.NetworkRepository.GetByIdAsync(first.Id, Arg.Any<CancellationToken>()).Returns(Result<Network>.Success(first));
        deps.NetworkRepository.GetByIdAsync(second.Id, Arg.Any<CancellationToken>()).Returns(Result<Network>.Success(second));
        deps.NetworkRepository.UpdateAsync(Arg.Any<Network>(), Arg.Any<CancellationToken>()).Returns(call => Result<Network>.Success(call.Arg<Network>()));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new BulkDeleteNetworksCommandHandler(deps.NetworkRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new BulkDeleteNetworksCommand([first.Id, second.Id]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AffectedCount.Should().Be(2);
        first.IsActive.Should().BeFalse();
        second.IsActive.Should().BeFalse();
        deps.AuditContext.Received(2).Add(Arg.Is<AuditContextEntry>(entry => entry.EntityType == nameof(Network) && entry.Action == AuditAction.Deleted));
    }

    [Fact]
    public async Task BulkDeleteNetworksCommand_WhenIdsEmpty_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(
            new BulkDeleteNetworksCommand(Array.Empty<Guid>()),
            new BulkDeleteNetworksCommandValidator(),
            new BulkOperationResponse(Guid.NewGuid(), 0));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task MergeNetworkCommandHandler_WhenValid_ReassignsDevicesAndAppendsAuditEntries()
    {
        var deps = CreateNetworkDeps();
        var source = new Network(Guid.NewGuid(), "Home LAN");
        var target = new Network(Guid.NewGuid(), "Guest WiFi");
        deps.NetworkRepository.GetByIdAsync(source.Id, Arg.Any<CancellationToken>()).Returns(Result<Network>.Success(source));
        deps.NetworkRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(Result<Network>.Success(target));
        deps.DeviceRepository.ReassignNetworkReferencesAsync(source.Id, target.Id, Arg.Any<CancellationToken>()).Returns(Result<int>.Success(2));
        deps.NetworkRepository.UpdateAsync(source, Arg.Any<CancellationToken>()).Returns(Result<Network>.Success(source));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new MergeNetworkCommandHandler(deps.NetworkRepository, deps.DeviceRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new MergeNetworkCommand(source.Id, target.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new MergeReferenceEntityResponse(2, source.Id, target.Id));
        source.IsActive.Should().BeFalse();
        await deps.DeviceRepository.Received(1).ReassignNetworkReferencesAsync(source.Id, target.Id, Arg.Any<CancellationToken>());
        deps.AuditContext.Received(2).Add(Arg.Any<AuditContextEntry>());
    }

    [Fact]
    public async Task MergeNetworkCommand_WhenIdsMatch_ReturnsValidationFailure()
    {
        var id = Guid.NewGuid();

        var result = await DeviceHandlerTestSupport.ValidateAsync(
            new MergeNetworkCommand(id, id),
            new MergeNetworkCommandValidator(),
            new MergeReferenceEntityResponse(0, Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    private static BrandBulkDeleteDeps CreateBrandDeps() => new(
        Substitute.For<IBrandRepository>(),
        Substitute.For<IUnitOfWork>(),
        Substitute.For<IAuditContext>());

    private static CategoryBulkDeleteDeps CreateCategoryDeps() => new(
        Substitute.For<ICategoryRepository>(),
        Substitute.For<IUnitOfWork>(),
        Substitute.For<IAuditContext>());

    private static LocationBulkDeleteDeps CreateLocationDeps() => new(
        Substitute.For<ILocationRepository>(),
        Substitute.For<IUnitOfWork>(),
        Substitute.For<IAuditContext>());

    private static NetworkBulkOpsDeps CreateNetworkDeps() => new(
        Substitute.For<INetworkRepository>(),
        Substitute.For<IDeviceRepository>(),
        Substitute.For<IUnitOfWork>(),
        Substitute.For<IAuditContext>());

    private sealed record BrandBulkDeleteDeps(
        IBrandRepository Repository,
        IUnitOfWork UnitOfWork,
        IAuditContext AuditContext);

    private sealed record CategoryBulkDeleteDeps(
        ICategoryRepository Repository,
        IUnitOfWork UnitOfWork,
        IAuditContext AuditContext);

    private sealed record LocationBulkDeleteDeps(
        ILocationRepository Repository,
        IUnitOfWork UnitOfWork,
        IAuditContext AuditContext);

    private sealed record NetworkBulkOpsDeps(
        INetworkRepository NetworkRepository,
        IDeviceRepository DeviceRepository,
        IUnitOfWork UnitOfWork,
        IAuditContext AuditContext);
}
