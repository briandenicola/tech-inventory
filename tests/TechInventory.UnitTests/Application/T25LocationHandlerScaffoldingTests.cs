using FluentAssertions;
using NSubstitute;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Locations;
using TechInventory.Application.Locations.Commands;
using TechInventory.Application.Locations.Queries;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.UnitTests.Application;

public sealed class T25LocationHandlerScaffoldingTests
{
    [Fact]
    public async Task CreateLocationCommandHandler_WhenValidInput_ReturnsSuccess()
    {
        var deps = CreateDependencies();
        deps.Repository.GetByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Location>.Failure(Error.NotFound("missing")));
        deps.Repository.AddAsync(Arg.Any<Location>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<Location>.Success(call.Arg<Location>()));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new CreateLocationCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new CreateLocationCommand("Hall Closet", LocationType.Storage), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Hall Closet");
        result.Value.Type.Should().Be(LocationType.Storage.ToString());
        deps.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Location) && entry.Action == AuditAction.Created));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateLocationCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(
            new CreateLocationCommand(string.Empty, LocationType.Home),
            new CreateLocationCommandValidator(),
            LocationResponse.FromEntity(DeviceHandlerTestSupport.CreateLocation()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task CreateLocationCommandHandler_WhenDuplicateNameExists_ReturnsConflictFailure()
    {
        var deps = CreateDependencies();
        deps.Repository.GetByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Location>.Success(DeviceHandlerTestSupport.CreateLocation()));
        var handler = new CreateLocationCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new CreateLocationCommand("Desk", LocationType.Home), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task UpdateLocationCommandHandler_WhenValidInput_ReturnsSuccessAndCapturesBeforeSnapshot()
    {
        var deps = CreateDependencies();
        var location = new Location(Guid.NewGuid(), "Desk", LocationType.Home);
        deps.Repository.GetByIdAsync(location.Id, Arg.Any<CancellationToken>()).Returns(Result<Location>.Success(location));
        deps.Repository.GetByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Location>.Failure(Error.NotFound("missing")));
        deps.Repository.UpdateAsync(location, Arg.Any<CancellationToken>()).Returns(Result<Location>.Success(location));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new UpdateLocationCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new UpdateLocationCommand(location.Id, "Office", LocationType.Storage), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Office");
        location.Type.Should().Be(LocationType.Storage);
        deps.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Location) &&
            entry.Action == AuditAction.Updated &&
            entry.BeforePayload != null &&
            entry.BeforePayload.ToString()!.Contains("Desk", StringComparison.Ordinal)));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateLocationCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(
            new UpdateLocationCommand(Guid.Empty, string.Empty, LocationType.Home),
            new UpdateLocationCommandValidator(),
            LocationResponse.FromEntity(DeviceHandlerTestSupport.CreateLocation()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task UpdateLocationCommandHandler_WhenLocationDoesNotExist_ReturnsNotFoundFailure()
    {
        var deps = CreateDependencies();
        var id = Guid.NewGuid();
        deps.Repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Result<Location>.Failure(Error.NotFound("missing")));
        var handler = new UpdateLocationCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new UpdateLocationCommand(id, "Office", LocationType.Home), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task UpdateLocationCommandHandler_WhenDuplicateNameBelongsToDifferentLocation_ReturnsConflictFailure()
    {
        var deps = CreateDependencies();
        var location = new Location(Guid.NewGuid(), "Desk", LocationType.Home);
        var other = new Location(Guid.NewGuid(), "Office", LocationType.Home);
        deps.Repository.GetByIdAsync(location.Id, Arg.Any<CancellationToken>()).Returns(Result<Location>.Success(location));
        deps.Repository.GetByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Location>.Success(other));
        var handler = new UpdateLocationCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new UpdateLocationCommand(location.Id, "Office", LocationType.Home), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task DeleteLocationCommandHandler_WhenLocationExists_DeactivatesAndCapturesBeforeSnapshot()
    {
        var deps = CreateDependencies();
        var location = new Location(Guid.NewGuid(), "Desk", LocationType.Home);
        deps.Repository.GetByIdAsync(location.Id, Arg.Any<CancellationToken>()).Returns(Result<Location>.Success(location));
        deps.Repository.UpdateAsync(location, Arg.Any<CancellationToken>()).Returns(Result<Location>.Success(location));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new DeleteLocationCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new DeleteLocationCommand(location.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        location.IsActive.Should().BeFalse();
        deps.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Location) &&
            entry.Action == AuditAction.Deleted &&
            entry.BeforePayload != null &&
            entry.BeforePayload.ToString()!.Contains("Desk", StringComparison.Ordinal)));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteLocationCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(new DeleteLocationCommand(Guid.Empty), new DeleteLocationCommandValidator());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task DeleteLocationCommandHandler_WhenLocationDoesNotExist_ReturnsNotFoundFailure()
    {
        var deps = CreateDependencies();
        var id = Guid.NewGuid();
        deps.Repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Result<Location>.Failure(Error.NotFound("missing")));
        var handler = new DeleteLocationCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new DeleteLocationCommand(id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task DeleteLocationCommandHandler_WhenLocationAlreadyInactive_ReturnsConflictFailure()
    {
        var deps = CreateDependencies();
        var location = DeviceHandlerTestSupport.CreateLocation(isActive: false);
        deps.Repository.GetByIdAsync(location.Id, Arg.Any<CancellationToken>()).Returns(Result<Location>.Success(location));
        var handler = new DeleteLocationCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new DeleteLocationCommand(location.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task GetLocationByIdQueryHandler_WhenLocationExists_ReturnsSuccess()
    {
        var deps = CreateDependencies();
        var location = DeviceHandlerTestSupport.CreateLocation();
        deps.Repository.GetByIdAsync(location.Id, Arg.Any<CancellationToken>()).Returns(Result<Location>.Success(location));
        var handler = new GetLocationByIdQueryHandler(deps.Repository);

        var result = await handler.Handle(new GetLocationByIdQuery(location.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(location.Id);
        result.Value.Name.Should().Be(location.Name);
    }

    [Fact]
    public async Task GetLocationByIdQueryHandler_WhenLocationDoesNotExist_ReturnsNotFoundFailure()
    {
        var deps = CreateDependencies();
        var id = Guid.NewGuid();
        deps.Repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Result<Location>.Failure(Error.NotFound("missing")));
        var handler = new GetLocationByIdQueryHandler(deps.Repository);

        var result = await handler.Handle(new GetLocationByIdQuery(id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task ListLocationsQueryHandler_WhenRequested_ReturnsAllItems()
    {
        var deps = CreateDependencies();
        var locations = new[]
        {
            new Location(Guid.NewGuid(), "Hall Closet", LocationType.Storage),
            new Location(Guid.NewGuid(), "Desk", LocationType.Home),
        };
        deps.Repository.ListAsync(false, Arg.Any<CancellationToken>()).Returns(locations);
        var handler = new ListLocationsQueryHandler(deps.Repository);

        var result = await handler.Handle(new ListLocationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Select(l => l.Name).Should().Contain(new[] { "Hall Closet", "Desk" });
    }

    private static LocationDeps CreateDependencies() => new(
        Substitute.For<ILocationRepository>(),
        Substitute.For<IUnitOfWork>(),
        Substitute.For<IAuditContext>());

    private sealed record LocationDeps(ILocationRepository Repository, IUnitOfWork UnitOfWork, IAuditContext AuditContext);
}
