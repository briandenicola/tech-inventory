using FluentAssertions;
using NSubstitute;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Networks;
using TechInventory.Application.Networks.Commands;
using TechInventory.Application.Networks.Queries;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.UnitTests.Application;

public sealed class T26NetworkHandlerScaffoldingTests
{
    [Fact]
    public async Task CreateNetworkCommandHandler_WhenValidInput_ReturnsSuccess()
    {
        var deps = CreateDependencies();
        deps.Repository.GetByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Network>.Failure(Error.NotFound("missing")));
        deps.Repository.AddAsync(Arg.Any<Network>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<Network>.Success(call.Arg<Network>()));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new CreateNetworkCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new CreateNetworkCommand("Home LAN", "Primary household network"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Home LAN");
        result.Value.Description.Should().Be("Primary household network");
        deps.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Network) && entry.Action == AuditAction.Created));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateNetworkCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(
            new CreateNetworkCommand(string.Empty),
            new CreateNetworkCommandValidator(),
            NetworkResponse.FromEntity(DeviceHandlerTestSupport.CreateNetwork()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task CreateNetworkCommandHandler_WhenDuplicateNameExists_ReturnsConflictFailure()
    {
        var deps = CreateDependencies();
        deps.Repository.GetByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Network>.Success(DeviceHandlerTestSupport.CreateNetwork()));
        var handler = new CreateNetworkCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new CreateNetworkCommand("Home LAN"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task UpdateNetworkCommandHandler_WhenValidInput_ReturnsSuccessAndCapturesBeforeSnapshot()
    {
        var deps = CreateDependencies();
        var network = new Network(Guid.NewGuid(), "Home LAN");
        deps.Repository.GetByIdAsync(network.Id, Arg.Any<CancellationToken>()).Returns(Result<Network>.Success(network));
        deps.Repository.GetByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Network>.Failure(Error.NotFound("missing")));
        deps.Repository.UpdateAsync(network, Arg.Any<CancellationToken>()).Returns(Result<Network>.Success(network));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new UpdateNetworkCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new UpdateNetworkCommand(network.Id, "Guest WiFi", "Guest segment"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Guest WiFi");
        network.Name.Should().Be("Guest WiFi");
        deps.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Network) &&
            entry.Action == AuditAction.Updated &&
            entry.BeforePayload != null &&
            entry.BeforePayload.ToString()!.Contains("Home LAN", StringComparison.Ordinal)));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateNetworkCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(
            new UpdateNetworkCommand(Guid.Empty, string.Empty),
            new UpdateNetworkCommandValidator(),
            NetworkResponse.FromEntity(DeviceHandlerTestSupport.CreateNetwork()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task UpdateNetworkCommandHandler_WhenNetworkDoesNotExist_ReturnsNotFoundFailure()
    {
        var deps = CreateDependencies();
        var id = Guid.NewGuid();
        deps.Repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Result<Network>.Failure(Error.NotFound("missing")));
        var handler = new UpdateNetworkCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new UpdateNetworkCommand(id, "Guest WiFi"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task UpdateNetworkCommandHandler_WhenDuplicateNameBelongsToDifferentNetwork_ReturnsConflictFailure()
    {
        var deps = CreateDependencies();
        var network = new Network(Guid.NewGuid(), "Home LAN");
        var other = new Network(Guid.NewGuid(), "Guest WiFi");
        deps.Repository.GetByIdAsync(network.Id, Arg.Any<CancellationToken>()).Returns(Result<Network>.Success(network));
        deps.Repository.GetByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Network>.Success(other));
        var handler = new UpdateNetworkCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new UpdateNetworkCommand(network.Id, "Guest WiFi"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task DeleteNetworkCommandHandler_WhenNetworkExists_DeactivatesAndCapturesBeforeSnapshot()
    {
        var deps = CreateDependencies();
        var network = new Network(Guid.NewGuid(), "Home LAN");
        deps.Repository.GetByIdAsync(network.Id, Arg.Any<CancellationToken>()).Returns(Result<Network>.Success(network));
        deps.Repository.UpdateAsync(network, Arg.Any<CancellationToken>()).Returns(Result<Network>.Success(network));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new DeleteNetworkCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new DeleteNetworkCommand(network.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        network.IsActive.Should().BeFalse();
        deps.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Network) &&
            entry.Action == AuditAction.Deleted &&
            entry.BeforePayload != null &&
            entry.BeforePayload.ToString()!.Contains("Home LAN", StringComparison.Ordinal)));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteNetworkCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(new DeleteNetworkCommand(Guid.Empty), new DeleteNetworkCommandValidator());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task DeleteNetworkCommandHandler_WhenNetworkDoesNotExist_ReturnsNotFoundFailure()
    {
        var deps = CreateDependencies();
        var id = Guid.NewGuid();
        deps.Repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Result<Network>.Failure(Error.NotFound("missing")));
        var handler = new DeleteNetworkCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new DeleteNetworkCommand(id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task DeleteNetworkCommandHandler_WhenNetworkAlreadyInactive_ReturnsConflictFailure()
    {
        var deps = CreateDependencies();
        var network = DeviceHandlerTestSupport.CreateNetwork(isActive: false);
        deps.Repository.GetByIdAsync(network.Id, Arg.Any<CancellationToken>()).Returns(Result<Network>.Success(network));
        var handler = new DeleteNetworkCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new DeleteNetworkCommand(network.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task GetNetworkByIdQueryHandler_WhenNetworkExists_ReturnsSuccess()
    {
        var deps = CreateDependencies();
        var network = DeviceHandlerTestSupport.CreateNetwork();
        deps.Repository.GetByIdAsync(network.Id, Arg.Any<CancellationToken>()).Returns(Result<Network>.Success(network));
        var handler = new GetNetworkByIdQueryHandler(deps.Repository);

        var result = await handler.Handle(new GetNetworkByIdQuery(network.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(network.Id);
        result.Value.Name.Should().Be(network.Name);
    }

    [Fact]
    public async Task GetNetworkByIdQueryHandler_WhenNetworkDoesNotExist_ReturnsNotFoundFailure()
    {
        var deps = CreateDependencies();
        var id = Guid.NewGuid();
        deps.Repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Result<Network>.Failure(Error.NotFound("missing")));
        var handler = new GetNetworkByIdQueryHandler(deps.Repository);

        var result = await handler.Handle(new GetNetworkByIdQuery(id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task ListNetworksQueryHandler_WhenRequested_ReturnsAllItems()
    {
        var deps = CreateDependencies();
        var networks = new[]
        {
            new Network(Guid.NewGuid(), "Home LAN"),
            new Network(Guid.NewGuid(), "Guest WiFi"),
        };
        deps.Repository.ListAsync(false, Arg.Any<CancellationToken>()).Returns(networks);
        var handler = new ListNetworksQueryHandler(deps.Repository);

        var result = await handler.Handle(new ListNetworksQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Select(n => n.Name).Should().Contain(new[] { "Home LAN", "Guest WiFi" });
    }

    private static NetworkDeps CreateDependencies() => new(
        Substitute.For<INetworkRepository>(),
        Substitute.For<IUnitOfWork>(),
        Substitute.For<IAuditContext>());

    private sealed record NetworkDeps(INetworkRepository Repository, IUnitOfWork UnitOfWork, IAuditContext AuditContext);
}
