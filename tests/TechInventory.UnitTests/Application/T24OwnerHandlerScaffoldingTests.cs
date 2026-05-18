using FluentAssertions;
using NSubstitute;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Owners;
using TechInventory.Application.Owners.Commands;
using TechInventory.Application.Owners.Queries;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.UnitTests.Application;

public sealed class T24OwnerHandlerScaffoldingTests
{
    [Fact]
    public async Task CreateOwnerCommandHandler_WhenValidInput_ReturnsSuccess()
    {
        var deps = CreateDependencies();
        deps.OwnerRepository.GetByNormalizedDisplayNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Owner>.Failure(Error.NotFound("missing")));
        deps.OwnerRepository.AddAsync(Arg.Any<Owner>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<Owner>.Success(call.Arg<Owner>()));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new CreateOwnerCommandHandler(deps.OwnerRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new CreateOwnerCommand("Ripley", OwnerRole.Admin), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DisplayName.Should().Be("Ripley");
        result.Value.Role.Should().Be(OwnerRole.Admin.ToString());
        deps.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Owner) && entry.Action == AuditAction.Created));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOwnerCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(
            new CreateOwnerCommand(string.Empty),
            new CreateOwnerCommandValidator(),
            OwnerResponse.FromEntity(DeviceHandlerTestSupport.CreateOwner()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task CreateOwnerCommandHandler_WhenDuplicateDisplayName_ReturnsConflictFailure()
    {
        var deps = CreateDependencies();
        deps.OwnerRepository.GetByNormalizedDisplayNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Owner>.Success(DeviceHandlerTestSupport.CreateOwner()));
        var handler = new CreateOwnerCommandHandler(deps.OwnerRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new CreateOwnerCommand("Ripley"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task UpdateOwnerCommandHandler_WhenValidInput_ReturnsSuccessAndCapturesBeforeSnapshot()
    {
        var deps = CreateDependencies();
        var owner = new Owner(Guid.NewGuid(), "Ripley", OwnerRole.Member);
        deps.OwnerRepository.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(Result<Owner>.Success(owner));
        deps.OwnerRepository.GetByNormalizedDisplayNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Owner>.Failure(Error.NotFound("missing")));
        deps.OwnerRepository.UpdateAsync(owner, Arg.Any<CancellationToken>()).Returns(Result<Owner>.Success(owner));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new UpdateOwnerCommandHandler(deps.OwnerRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new UpdateOwnerCommand(owner.Id, "Ellen", OwnerRole.Admin), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DisplayName.Should().Be("Ellen");
        owner.Role.Should().Be(OwnerRole.Admin);
        deps.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Owner) &&
            entry.Action == AuditAction.Updated &&
            entry.BeforePayload != null &&
            entry.BeforePayload.ToString()!.Contains("Ripley", StringComparison.Ordinal)));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateOwnerCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(
            new UpdateOwnerCommand(Guid.Empty, string.Empty, OwnerRole.Member),
            new UpdateOwnerCommandValidator(),
            OwnerResponse.FromEntity(DeviceHandlerTestSupport.CreateOwner()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task UpdateOwnerCommandHandler_WhenOwnerDoesNotExist_ReturnsNotFoundFailure()
    {
        var deps = CreateDependencies();
        var id = Guid.NewGuid();
        deps.OwnerRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Result<Owner>.Failure(Error.NotFound("missing")));
        var handler = new UpdateOwnerCommandHandler(deps.OwnerRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new UpdateOwnerCommand(id, "Ellen", OwnerRole.Member), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task UpdateOwnerCommandHandler_WhenDuplicateDisplayNameBelongsToDifferentOwner_ReturnsConflictFailure()
    {
        var deps = CreateDependencies();
        var owner = new Owner(Guid.NewGuid(), "Ripley");
        var other = new Owner(Guid.NewGuid(), "Ellen");
        deps.OwnerRepository.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(Result<Owner>.Success(owner));
        deps.OwnerRepository.GetByNormalizedDisplayNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Owner>.Success(other));
        var handler = new UpdateOwnerCommandHandler(deps.OwnerRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new UpdateOwnerCommand(owner.Id, "Ellen", OwnerRole.Member), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task DeleteOwnerCommandHandler_WhenOwnerHasNoDevices_DeactivatesAndCapturesBeforeSnapshot()
    {
        var deps = CreateDependencies();
        var owner = new Owner(Guid.NewGuid(), "Ripley");
        deps.OwnerRepository.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(Result<Owner>.Success(owner));
        deps.DeviceRepository.ListAsync(Arg.Any<DeviceListCriteria>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Device>(Array.Empty<Device>(), totalCount: 0, page: 1, pageSize: 1));
        deps.OwnerRepository.UpdateAsync(owner, Arg.Any<CancellationToken>()).Returns(Result<Owner>.Success(owner));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new DeleteOwnerCommandHandler(deps.OwnerRepository, deps.DeviceRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new DeleteOwnerCommand(owner.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        owner.IsActive.Should().BeFalse();
        deps.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Owner) &&
            entry.Action == AuditAction.Deleted &&
            entry.BeforePayload != null &&
            entry.BeforePayload.ToString()!.Contains("Ripley", StringComparison.Ordinal)));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteOwnerCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(new DeleteOwnerCommand(Guid.Empty), new DeleteOwnerCommandValidator());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task DeleteOwnerCommandHandler_WhenOwnerDoesNotExist_ReturnsNotFoundFailure()
    {
        var deps = CreateDependencies();
        var id = Guid.NewGuid();
        deps.OwnerRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Result<Owner>.Failure(Error.NotFound("missing")));
        var handler = new DeleteOwnerCommandHandler(deps.OwnerRepository, deps.DeviceRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new DeleteOwnerCommand(id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task DeleteOwnerCommandHandler_WhenOwnerHasAssignedDevices_ReturnsConflictFailure()
    {
        var deps = CreateDependencies();
        var owner = new Owner(Guid.NewGuid(), "Ripley");
        var device = DeviceHandlerTestSupport.CreateDevice(ownerId: owner.Id);
        deps.OwnerRepository.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(Result<Owner>.Success(owner));
        deps.DeviceRepository.ListAsync(Arg.Any<DeviceListCriteria>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Device>(new[] { device }, totalCount: 1, page: 1, pageSize: 1));
        var handler = new DeleteOwnerCommandHandler(deps.OwnerRepository, deps.DeviceRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new DeleteOwnerCommand(owner.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
        await deps.OwnerRepository.DidNotReceive().UpdateAsync(Arg.Any<Owner>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOwnerByIdQueryHandler_WhenOwnerExists_ReturnsSuccess()
    {
        var deps = CreateDependencies();
        var owner = DeviceHandlerTestSupport.CreateOwner();
        deps.OwnerRepository.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(Result<Owner>.Success(owner));
        var handler = new GetOwnerByIdQueryHandler(deps.OwnerRepository);

        var result = await handler.Handle(new GetOwnerByIdQuery(owner.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(owner.Id);
        result.Value.DisplayName.Should().Be(owner.DisplayName);
    }

    [Fact]
    public async Task GetOwnerByIdQueryHandler_WhenOwnerDoesNotExist_ReturnsNotFoundFailure()
    {
        var deps = CreateDependencies();
        var id = Guid.NewGuid();
        deps.OwnerRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Result<Owner>.Failure(Error.NotFound("missing")));
        var handler = new GetOwnerByIdQueryHandler(deps.OwnerRepository);

        var result = await handler.Handle(new GetOwnerByIdQuery(id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task ListOwnersQueryHandler_WhenRequested_ReturnsAllItems()
    {
        var deps = CreateDependencies();
        var owners = new[]
        {
            new Owner(Guid.NewGuid(), "Ripley"),
            new Owner(Guid.NewGuid(), "Ellen"),
        };
        deps.OwnerRepository.ListAsync(false, Arg.Any<CancellationToken>()).Returns(owners);
        var handler = new ListOwnersQueryHandler(deps.OwnerRepository);

        var result = await handler.Handle(new ListOwnersQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Select(o => o.DisplayName).Should().Contain(new[] { "Ripley", "Ellen" });
    }

    private static OwnerDeps CreateDependencies() => new(
        Substitute.For<IOwnerRepository>(),
        Substitute.For<IDeviceRepository>(),
        Substitute.For<IUnitOfWork>(),
        Substitute.For<IAuditContext>());

    private sealed record OwnerDeps(
        IOwnerRepository OwnerRepository,
        IDeviceRepository DeviceRepository,
        IUnitOfWork UnitOfWork,
        IAuditContext AuditContext);
}
