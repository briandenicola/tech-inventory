using FluentAssertions;
using NSubstitute;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Brands.Commands;
using TechInventory.Application.Categories.Commands;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Locations.Commands;
using TechInventory.Application.Merges;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.UnitTests.Application;

public sealed class ReferenceMergeCommandHandlerTests
{
    [Fact]
    public async Task MergeBrandCommandHandler_WhenValid_ReassignsDevicesAndAppendsAuditEntries()
    {
        var deps = CreateBrandDependencies();
        var source = new Brand(Guid.NewGuid(), "Apple");
        var target = new Brand(Guid.NewGuid(), "APPLE");
        deps.BrandRepository.GetByIdAsync(source.Id, Arg.Any<CancellationToken>()).Returns(Result<Brand>.Success(source));
        deps.BrandRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(Result<Brand>.Success(target));
        deps.DeviceRepository.ReassignBrandReferencesAsync(source.Id, target.Id, Arg.Any<CancellationToken>())
            .Returns(Result<int>.Success(3));
        deps.BrandRepository.UpdateAsync(source, Arg.Any<CancellationToken>()).Returns(Result<Brand>.Success(source));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new MergeBrandCommandHandler(deps.BrandRepository, deps.DeviceRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new MergeBrandCommand(source.Id, target.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new MergeReferenceEntityResponse(3, source.Id, target.Id));
        source.IsActive.Should().BeFalse();
        await deps.DeviceRepository.Received(1).ReassignBrandReferencesAsync(source.Id, target.Id, Arg.Any<CancellationToken>());
        deps.AuditContext.Received(1).Add(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityId == target.Id.ToString() &&
            entry.Action == AuditAction.Updated));
        deps.AuditContext.Received(1).Add(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityId == source.Id.ToString() &&
            entry.Action == AuditAction.Deleted &&
            entry.BeforePayload != null));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MergeBrandCommand_WhenIdsMatch_ReturnsValidationFailure()
    {
        var id = Guid.NewGuid();

        var result = await DeviceHandlerTestSupport.ValidateAsync(
            new MergeBrandCommand(id, id),
            new MergeBrandCommandValidator(),
            new MergeReferenceEntityResponse(0, Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task MergeCategoryCommandHandler_WhenTargetIsDescendant_ReturnsConflictFailure()
    {
        var deps = CreateCategoryDependencies();
        var source = new Category(Guid.NewGuid(), "Living Room");
        var target = new Category(Guid.NewGuid(), "Speakers", source.Id, 2);
        deps.CategoryRepository.GetByIdAsync(source.Id, Arg.Any<CancellationToken>()).Returns(Result<Category>.Success(source));
        deps.CategoryRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(Result<Category>.Success(target));
        deps.CategoryRepository.ListAsync(true, Arg.Any<CancellationToken>()).Returns([source, target]);
        var handler = new MergeCategoryCommandHandler(deps.CategoryRepository, deps.DeviceRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new MergeCategoryCommand(source.Id, target.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
        await deps.DeviceRepository.DidNotReceive().ReassignCategoryReferencesAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await deps.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MergeCategoryCommandHandler_WhenValid_ReparentsDescendantsAndReturnsMergedCount()
    {
        var deps = CreateCategoryDependencies();
        var source = new Category(Guid.NewGuid(), "Living Room");
        var target = new Category(Guid.NewGuid(), "Family Room");
        var child = new Category(Guid.NewGuid(), "TVs", source.Id, 2);
        var grandchild = new Category(Guid.NewGuid(), "Projectors", child.Id, 3);
        deps.CategoryRepository.GetByIdAsync(source.Id, Arg.Any<CancellationToken>()).Returns(Result<Category>.Success(source));
        deps.CategoryRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(Result<Category>.Success(target));
        deps.CategoryRepository.ListAsync(true, Arg.Any<CancellationToken>()).Returns([source, target, child, grandchild]);
        deps.DeviceRepository.ReassignCategoryReferencesAsync(source.Id, target.Id, Arg.Any<CancellationToken>())
            .Returns(Result<int>.Success(2));
        deps.CategoryRepository.UpdateAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<Category>.Success(call.Arg<Category>()));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new MergeCategoryCommandHandler(deps.CategoryRepository, deps.DeviceRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new MergeCategoryCommand(source.Id, target.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new MergeReferenceEntityResponse(2, source.Id, target.Id));
        child.ParentId.Should().Be(target.Id);
        child.Depth.Should().Be(2);
        grandchild.ParentId.Should().Be(child.Id);
        grandchild.Depth.Should().Be(3);
        source.IsActive.Should().BeFalse();
        await deps.DeviceRepository.Received(1).ReassignCategoryReferencesAsync(source.Id, target.Id, Arg.Any<CancellationToken>());
        await deps.CategoryRepository.Received(3).UpdateAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
        deps.AuditContext.Received(2).Add(Arg.Any<AuditContextEntry>());
    }

    [Fact]
    public async Task MergeLocationCommandHandler_WhenNoReferences_ReturnsZeroMergedCount()
    {
        var deps = CreateLocationDependencies();
        var source = new Location(Guid.NewGuid(), "Living Room", LocationType.Home);
        var target = new Location(Guid.NewGuid(), "Family Room", LocationType.Home);
        deps.LocationRepository.GetByIdAsync(source.Id, Arg.Any<CancellationToken>()).Returns(Result<Location>.Success(source));
        deps.LocationRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(Result<Location>.Success(target));
        deps.DeviceRepository.ReassignLocationReferencesAsync(source.Id, target.Id, Arg.Any<CancellationToken>())
            .Returns(Result<int>.Success(0));
        deps.LocationRepository.UpdateAsync(source, Arg.Any<CancellationToken>()).Returns(Result<Location>.Success(source));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new MergeLocationCommandHandler(deps.LocationRepository, deps.DeviceRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new MergeLocationCommand(source.Id, target.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new MergeReferenceEntityResponse(0, source.Id, target.Id));
        source.IsActive.Should().BeFalse();
        await deps.DeviceRepository.Received(1).ReassignLocationReferencesAsync(source.Id, target.Id, Arg.Any<CancellationToken>());
        deps.AuditContext.Received(2).Add(Arg.Any<AuditContextEntry>());
    }

    private static BrandMergeDeps CreateBrandDependencies() => new(
        Substitute.For<IBrandRepository>(),
        Substitute.For<IDeviceRepository>(),
        Substitute.For<IUnitOfWork>(),
        Substitute.For<IAuditContext>());

    private static CategoryMergeDeps CreateCategoryDependencies() => new(
        Substitute.For<ICategoryRepository>(),
        Substitute.For<IDeviceRepository>(),
        Substitute.For<IUnitOfWork>(),
        Substitute.For<IAuditContext>());

    private static LocationMergeDeps CreateLocationDependencies() => new(
        Substitute.For<ILocationRepository>(),
        Substitute.For<IDeviceRepository>(),
        Substitute.For<IUnitOfWork>(),
        Substitute.For<IAuditContext>());

    private sealed record BrandMergeDeps(
        IBrandRepository BrandRepository,
        IDeviceRepository DeviceRepository,
        IUnitOfWork UnitOfWork,
        IAuditContext AuditContext);

    private sealed record CategoryMergeDeps(
        ICategoryRepository CategoryRepository,
        IDeviceRepository DeviceRepository,
        IUnitOfWork UnitOfWork,
        IAuditContext AuditContext);

    private sealed record LocationMergeDeps(
        ILocationRepository LocationRepository,
        IDeviceRepository DeviceRepository,
        IUnitOfWork UnitOfWork,
        IAuditContext AuditContext);
}
